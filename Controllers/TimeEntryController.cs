using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPlanner.Data;
using ProjectPlanner.Models;

namespace ProjectPlanner.Controllers;

/// <summary>
/// Stundenerfassung: Einstempeln, Ausstempeln, manuelle Einträge, Übersicht.
/// Lehrer haben nur Lesezugriff.
/// </summary>
[ApiController]
[Route("api/projects/{projectId:int}/tasks/{taskId:int}/time")]
[Authorize]
public class TimeEntryController : ControllerBase
{
    private readonly AppDbContext         _db;
    private readonly UserManager<AppUser> _userManager;

    public TimeEntryController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db          = db;
        _userManager = userManager;
    }

    // ── Alle Zeiteinträge einer Task ─────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(int projectId, int taskId)
    {
        var entries = await _db.TimeEntries
            .Include(te => te.User)
            .Where(te => te.TaskId == taskId && te.ProjectId == projectId)
            .OrderByDescending(te => te.StartTime)
            .Select(te => new
            {
                te.Id, te.StartTime, te.EndTime, te.DurationHours,
                te.Description, te.IsManual, te.CreatedAt,
                user = new { te.User.Id, te.User.FullName, te.User.Email }
            })
            .ToListAsync();

        return Ok(entries);
    }

    // ── Einstempeln (Start) ──────────────────────────────────────────────────
    [HttpPost("stamp-in")]
    [Authorize(Roles = "Admin,Student")]
    public async Task<IActionResult> StampIn(int projectId, int taskId, [FromBody] StampInDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Prüfen ob noch eine offene Stempelung läuft
        var running = await _db.TimeEntries
            .FirstOrDefaultAsync(te => te.UserId == userId && te.EndTime == null);
        if (running != null)
            return Conflict(new { error = "Es läuft bereits eine Stempelung. Bitte zuerst ausstempeln.", runningEntryId = running.Id });

        var task = await _db.Tasks.FindAsync(taskId);
        if (task == null || task.ProjectId != projectId) return NotFound(new { error = "Aufgabe nicht gefunden." });

        var entry = new TimeEntry
        {
            UserId      = userId,
            TaskId      = taskId,
            ProjectId   = projectId,
            StartTime   = DateTime.UtcNow,
            Description = dto.Description,
            IsManual    = false,
            CreatedAt   = DateTime.UtcNow
        };

        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();

        return Ok(new { entryId = entry.Id, startTime = entry.StartTime, message = "Eingestempelt." });
    }

    // ── Ausstempeln (Ende) ───────────────────────────────────────────────────
    [HttpPost("stamp-out")]
    [Authorize(Roles = "Admin,Student")]
    public async Task<IActionResult> StampOut(int projectId, int taskId, [FromBody] StampOutDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var entry = await _db.TimeEntries
            .FirstOrDefaultAsync(te => te.UserId == userId && te.TaskId == taskId && te.EndTime == null);

        if (entry == null)
            return NotFound(new { error = "Keine laufende Stempelung für diese Aufgabe gefunden." });

        entry.EndTime     = DateTime.UtcNow;
        entry.Description = dto.Description ?? entry.Description;
        entry.DurationHours = (decimal)(entry.EndTime.Value - entry.StartTime).TotalHours;

        await _db.SaveChangesAsync();
        await RecalculateTaskDurations(taskId, projectId);

        return Ok(new
        {
            entryId       = entry.Id,
            startTime     = entry.StartTime,
            endTime       = entry.EndTime,
            durationHours = entry.DurationHours,
            message       = "Ausgestempelt."
        });
    }

    // ── Manuellen Zeiteintrag hinzufügen ─────────────────────────────────────
    [HttpPost("manual")]
    [Authorize(Roles = "Admin,Student")]
    public async Task<IActionResult> AddManual(int projectId, int taskId, [FromBody] ManualEntryDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var task = await _db.Tasks.FindAsync(taskId);
        if (task == null || task.ProjectId != projectId) return NotFound(new { error = "Aufgabe nicht gefunden." });

        if (dto.DurationHours <= 0)
            return BadRequest(new { error = "Dauer muss größer als 0 Stunden sein." });

        var entry = new TimeEntry
        {
            UserId        = userId,
            TaskId        = taskId,
            ProjectId     = projectId,
            StartTime     = dto.StartTime,
            EndTime       = dto.StartTime.AddHours((double)dto.DurationHours),
            DurationHours = dto.DurationHours,
            Description   = dto.Description,
            IsManual      = true,
            CreatedAt     = DateTime.UtcNow
        };

        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();
        await RecalculateTaskDurations(taskId, projectId);

        return Ok(new { entryId = entry.Id, message = "Manueller Zeiteintrag gespeichert." });
    }

    // ── Zeiteintrag löschen ──────────────────────────────────────────────────
    [HttpDelete("{entryId:int}")]
    [Authorize(Roles = "Admin,Student")]
    public async Task<IActionResult> Delete(int projectId, int taskId, int entryId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role   = User.FindFirstValue(ClaimTypes.Role)!;

        var entry = await _db.TimeEntries.FindAsync(entryId);
        if (entry == null || entry.TaskId != taskId) return NotFound();
        if (entry.UserId != userId && role != "Admin") return Forbid();

        _db.TimeEntries.Remove(entry);
        await _db.SaveChangesAsync();
        await RecalculateTaskDurations(taskId, projectId);

        return NoContent();
    }

    // ── Laufende Stempelung des aktuellen Benutzers ──────────────────────────
    [HttpGet("running")]
    public async Task<IActionResult> GetRunning(int projectId, int taskId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var entry  = await _db.TimeEntries
            .FirstOrDefaultAsync(te => te.UserId == userId && te.TaskId == taskId && te.EndTime == null);

        if (entry == null) return Ok(null);
        return Ok(new { entry.Id, entry.StartTime, entry.Description });
    }

    // ── Zeitzusammenfassung pro Task ─────────────────────────────────────────
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(int projectId, int taskId)
    {
        var entries = await _db.TimeEntries
            .Include(te => te.User)
            .Where(te => te.TaskId == taskId && te.ProjectId == projectId && te.DurationHours != null)
            .ToListAsync();

        var totalProjectHours = await _db.TimeEntries
            .Where(te => te.ProjectId == projectId && te.DurationHours != null)
            .SumAsync(te => te.DurationHours ?? 0);

        var totalTaskHours = entries.Sum(te => te.DurationHours ?? 0);

        var perUser = entries
            .GroupBy(te => new { te.UserId, te.User.FullName })
            .Select(g => new
            {
                userId        = g.Key.UserId,
                fullName      = g.Key.FullName,
                totalHours    = g.Sum(te => te.DurationHours ?? 0),
                sharePercent  = totalProjectHours > 0
                    ? Math.Round((g.Sum(te => te.DurationHours ?? 0) / totalProjectHours) * 100, 2)
                    : 0
            })
            .ToList();

        return Ok(new
        {
            totalTaskHours,
            totalProjectHours,
            workSharePercent = totalProjectHours > 0
                ? Math.Round((totalTaskHours / totalProjectHours) * 100, 2)
                : 0,
            perUser
        });
    }

    // ── Hilfsmethode: ActualDuration und WorkSharePercent neu berechnen ──────
    private async Task RecalculateTaskDurations(int taskId, int projectId)
    {
        var task = await _db.Tasks.FindAsync(taskId);
        if (task == null) return;

        // Tatsächliche Dauer = Summe aller abgeschlossenen Einträge dieser Task
        task.ActualDuration = await _db.TimeEntries
            .Where(te => te.TaskId == taskId && te.DurationHours != null)
            .SumAsync(te => te.DurationHours ?? 0);

        // Gesamtstunden im Projekt
        var totalProjectHours = await _db.TimeEntries
            .Where(te => te.ProjectId == projectId && te.DurationHours != null)
            .SumAsync(te => te.DurationHours ?? 0);

        // Arbeitsanteil in %
        task.WorkSharePercent = totalProjectHours > 0
            ? Math.Round((task.ActualDuration / totalProjectHours) * 100, 2)
            : 0;

        // Auch alle anderen Tasks des Projekts neu gewichten
        var allTaskIds = await _db.Tasks
            .Where(t => t.ProjectId == projectId && t.Id != taskId)
            .Select(t => t.Id)
            .ToListAsync();

        foreach (var otherId in allTaskIds)
        {
            var other = await _db.Tasks.FindAsync(otherId);
            if (other == null) continue;
            other.WorkSharePercent = totalProjectHours > 0
                ? Math.Round((other.ActualDuration / totalProjectHours) * 100, 2)
                : 0;
        }

        await _db.SaveChangesAsync();
    }
}

public record StampInDto(string? Description = null);
public record StampOutDto(string? Description = null);
public record ManualEntryDto(DateTime StartTime, decimal DurationHours, string? Description = null);
