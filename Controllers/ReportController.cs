using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPlanner.Data;

namespace ProjectPlanner.Controllers;

/// <summary>
/// Reporting: Arbeitszeiten, Stundenübersichten, Kosten pro Projekt.
/// Lehrer und Admin sehen alle Daten; Schüler nur ihre eigenen.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportController(AppDbContext db) => _db = db;

    // ── Stundenübersicht pro Projekt ─────────────────────────────────────────
    [HttpGet("project/{projectId:int}")]
    public async Task<IActionResult> ProjectReport(int projectId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role   = User.FindFirstValue(ClaimTypes.Role)!;

        // Zugriffsprüfung für Schüler
        if (role == "Student")
        {
            var hasAccess = await _db.Projects.AnyAsync(p =>
                p.Id == projectId && (p.OwnerId == userId || p.Members.Any(m => m.UserId == userId)));
            if (!hasAccess) return Forbid();
        }

        var tasks = await _db.Tasks
            .Where(t => t.ProjectId == projectId)
            .Select(t => new
            {
                t.Id, t.Title, t.PlannedDuration, t.ActualDuration, t.WorkSharePercent, t.Status
            })
            .ToListAsync();

        var entries = await _db.TimeEntries
            .Include(te => te.User)
            .Where(te => te.ProjectId == projectId && te.DurationHours != null)
            .ToListAsync();

        var totalHours = entries.Sum(te => te.DurationHours ?? 0);

        var perMember = entries
            .GroupBy(te => new { te.UserId, te.User.FullName })
            .Select(g => new
            {
                userId       = g.Key.UserId,
                fullName     = g.Key.FullName,
                totalHours   = g.Sum(te => te.DurationHours ?? 0),
                sharePercent = totalHours > 0
                    ? Math.Round((g.Sum(te => te.DurationHours ?? 0) / totalHours) * 100, 2)
                    : 0
            })
            .ToList();

        return Ok(new
        {
            projectId,
            totalHours,
            tasks,
            perMember
        });
    }

    // ── Eigene Zeiteinträge (Schüler-Ansicht) ────────────────────────────────
    [HttpGet("my-time")]
    public async Task<IActionResult> MyTime([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var query = _db.TimeEntries
            .Include(te => te.Task)
            .Include(te => te.Project)
            .Where(te => te.UserId == userId && te.DurationHours != null);

        if (from.HasValue) query = query.Where(te => te.StartTime >= from.Value);
        if (to.HasValue)   query = query.Where(te => te.StartTime <= to.Value);

        var entries = await query
            .OrderByDescending(te => te.StartTime)
            .Select(te => new
            {
                te.Id, te.StartTime, te.EndTime, te.DurationHours, te.Description, te.IsManual,
                taskTitle   = te.Task.Title,
                projectName = te.Project.Name,
                te.ProjectId, te.TaskId
            })
            .ToListAsync();

        return Ok(new
        {
            totalHours = entries.Sum(e => e.DurationHours ?? 0),
            entries
        });
    }

    // ── Alle Zeiten aller Benutzer (nur Admin/Teacher) ───────────────────────
    [HttpGet("all-time")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> AllTime([FromQuery] int? projectId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var query = _db.TimeEntries
            .Include(te => te.User)
            .Include(te => te.Task)
            .Include(te => te.Project)
            .Where(te => te.DurationHours != null);

        if (projectId.HasValue) query = query.Where(te => te.ProjectId == projectId.Value);
        if (from.HasValue)      query = query.Where(te => te.StartTime >= from.Value);
        if (to.HasValue)        query = query.Where(te => te.StartTime <= to.Value);

        var entries = await query
            .OrderByDescending(te => te.StartTime)
            .Select(te => new
            {
                te.Id, te.StartTime, te.EndTime, te.DurationHours, te.Description, te.IsManual,
                user        = new { te.User.Id, te.User.FullName },
                taskTitle   = te.Task.Title,
                projectName = te.Project.Name,
                te.ProjectId, te.TaskId
            })
            .ToListAsync();

        return Ok(new
        {
            totalHours = entries.Sum(e => e.DurationHours ?? 0),
            entries
        });
    }
}
