using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPlanner.Data;
using ProjectPlanner.Models;
using System.Security.Claims;

namespace ProjectPlanner.Controllers;

/// <summary>
/// Verwaltung von Mehrfachzuweisungen zu Aufgaben mit Prozentangaben
/// </summary>
[ApiController]
[Route("api/tasks/{taskId:int}/assignments")]
[Authorize]
public class TaskAssignmentController : ControllerBase
{
    private readonly AppDbContext _db;

    public TaskAssignmentController(AppDbContext db) => _db = db;

    // ── Alle Zuweisungen einer Aufgabe abrufen ────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAssignments(int taskId)
    {
        var task = await _db.Tasks
            .Include(t => t.TaskAssignments)
            .ThenInclude(ta => ta.User)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null) return NotFound(new { error = "Aufgabe nicht gefunden" });

        var assignments = task.TaskAssignments.Select(ta => new
        {
            ta.Id,
            ta.TaskId,
            ta.UserId,
            userName = ta.User.FullName,
            userShortName = ta.User.ShortName,
            ta.Percentage,
            ta.AssignedAt
        });

        return Ok(assignments);
    }

    // ── Zuweisung hinzufügen ───────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> AddAssignment(int taskId, AssignmentDto dto)
    {
        var task = await _db.Tasks
            .Include(t => t.TaskAssignments)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null) return NotFound(new { error = "Aufgabe nicht gefunden" });

        // Prüfe ob User existiert
        var user = await _db.Users.FindAsync(dto.UserId);
        if (user == null) return BadRequest(new { error = "Benutzer nicht gefunden" });

        // Prüfe ob bereits zugewiesen
        var existing = task.TaskAssignments.FirstOrDefault(ta => ta.UserId == dto.UserId);
        if (existing != null)
            return BadRequest(new { error = "Benutzer ist bereits dieser Aufgabe zugewiesen" });

        // Validierung: Gesamtprozent <= 100
        var currentTotal = task.TaskAssignments.Sum(ta => ta.Percentage);
        if (currentTotal + dto.Percentage > 100)
            return BadRequest(new { error = $"Gesamtprozentsatz würde {currentTotal + dto.Percentage}% überschreiten (max. 100%)" });

        var assignment = new TaskAssignment
        {
            TaskId = taskId,
            UserId = dto.UserId,
            Percentage = dto.Percentage,
            AssignedAt = DateTime.UtcNow
        };

        _db.TaskAssignments.Add(assignment);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            assignment.Id,
            assignment.TaskId,
            assignment.UserId,
            userName = user.FullName,
            userShortName = user.ShortName,
            assignment.Percentage,
            assignment.AssignedAt
        });
    }

    // ── Zuweisung aktualisieren (Prozentsatz ändern) ───────────────────────
    [HttpPut("{assignmentId:int}")]
    public async Task<IActionResult> UpdateAssignment(int taskId, int assignmentId, UpdateAssignmentDto dto)
    {
        var assignment = await _db.TaskAssignments
            .Include(ta => ta.Task)
            .ThenInclude(t => t.TaskAssignments)
            .Include(ta => ta.User)
            .FirstOrDefaultAsync(ta => ta.Id == assignmentId && ta.TaskId == taskId);

        if (assignment == null) return NotFound(new { error = "Zuweisung nicht gefunden" });

        // Validierung: Gesamtprozent <= 100 (ohne die aktuelle Zuweisung)
        var othersTotal = assignment.Task.TaskAssignments
            .Where(ta => ta.Id != assignmentId)
            .Sum(ta => ta.Percentage);

        if (othersTotal + dto.Percentage > 100)
            return BadRequest(new { error = $"Gesamtprozentsatz würde {othersTotal + dto.Percentage}% überschreiten (max. 100%)" });

        assignment.Percentage = dto.Percentage;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            assignment.Id,
            assignment.TaskId,
            assignment.UserId,
            userName = assignment.User.FullName,
            userShortName = assignment.User.ShortName,
            assignment.Percentage,
            assignment.AssignedAt
        });
    }

    // ── Zuweisung löschen ──────────────────────────────────────────────────
    [HttpDelete("{assignmentId:int}")]
    public async Task<IActionResult> DeleteAssignment(int taskId, int assignmentId)
    {
        var assignment = await _db.TaskAssignments
            .FirstOrDefaultAsync(ta => ta.Id == assignmentId && ta.TaskId == taskId);

        if (assignment == null) return NotFound(new { error = "Zuweisung nicht gefunden" });

        _db.TaskAssignments.Remove(assignment);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Zuweisung entfernt" });
    }

    // ── Batch-Update: Alle Zuweisungen auf einmal setzen ──────────────────
    [HttpPut]
    public async Task<IActionResult> SetAllAssignments(int taskId, BatchAssignmentsDto dto)
    {
        var task = await _db.Tasks
            .Include(t => t.TaskAssignments)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null) return NotFound(new { error = "Aufgabe nicht gefunden" });

        // Validierung: Gesamtprozent = 100 (optional, kann auch < 100 sein)
        var totalPercentage = dto.Assignments.Sum(a => a.Percentage);
        if (totalPercentage > 100)
            return BadRequest(new { error = $"Gesamtprozentsatz {totalPercentage}% überschreitet 100%" });

        // Lösche alle bestehenden Zuweisungen
        _db.TaskAssignments.RemoveRange(task.TaskAssignments);

        // Erstelle neue Zuweisungen
        foreach (var assignDto in dto.Assignments)
        {
            var user = await _db.Users.FindAsync(assignDto.UserId);
            if (user == null) continue; // Skip ungültige User

            _db.TaskAssignments.Add(new TaskAssignment
            {
                TaskId = taskId,
                UserId = assignDto.UserId,
                Percentage = assignDto.Percentage,
                AssignedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        // Reload mit User-Daten
        var updatedTask = await _db.Tasks
            .Include(t => t.TaskAssignments)
            .ThenInclude(ta => ta.User)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        var assignments = updatedTask!.TaskAssignments.Select(ta => new
        {
            ta.Id,
            ta.UserId,
            userName = ta.User.FullName,
            userShortName = ta.User.ShortName,
            ta.Percentage
        });

        return Ok(new { message = "Zuweisungen aktualisiert", assignments });
    }
}

public record AssignmentDto(string UserId, decimal Percentage);
public record UpdateAssignmentDto(decimal Percentage);
public record BatchAssignmentsDto(List<AssignmentDto> Assignments);
