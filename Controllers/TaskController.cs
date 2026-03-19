using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPlanner.Data;
using ProjectPlanner.Models;

namespace ProjectPlanner.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/tasks")]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public TaskController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // ── Gantt-Daten (Tasks + Links) ──────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetGanttData(int projectId)
    {
        var tasks = await _db.Tasks
            .Include(t => t.Assignee)
            .Include(t => t.TaskAssignments)
            .ThenInclude(ta => ta.User)
            .Where(t => t.ProjectId == projectId)
            .Select(t => new
            {
                id = t.Id,
                text = t.Title,
                start_date = t.StartDate.ToString("yyyy-MM-dd"),
                end_date = t.EndDate.ToString("yyyy-MM-dd"),
                progress = t.Progress / 100.0,
                parent = t.ParentId ?? 0,
                priority = t.Priority,
                status = t.Status,
                // dhtmlxGantt: "milestone" type kann keine Kinder haben -> "project" verwenden
                // damit Verschachtelung erhalten bleibt; isMilestone-Flag für visuelle Darstellung
                type = t.IsMilestone ? "project" : "task",
                isMilestone = t.IsMilestone,
                note = t.Note,
                // Legacy support
                assigneeId = t.AssigneeId,
                assigneeName = t.Assignee != null ? t.Assignee.FullName : null,
                // Neue Mehrfachzuweisung
                assignments = t.TaskAssignments.Select(ta => new {
                    userId = ta.UserId,
                    userName = ta.User.FullName,
                    shortName = ta.User.ShortName,
                    percentage = ta.Percentage
                }).ToList(),
                plannedDuration = t.PlannedDuration,
                actualDuration = t.ActualDuration,
                workSharePercent = t.WorkSharePercent,
                color = t.Priority == "High" ? "#E74C3C"
                                : t.Priority == "Low" ? "#27AE60"
                                : (string?)null
            })
            .ToListAsync();

        var links = await _db.TaskLinks
            .Where(l => l.ProjectId == projectId)
            .Select(l => new { id = l.Id, source = l.Source, target = l.Target, type = l.Type })
            .ToListAsync();

        return Ok(new { data = tasks, links });
    }

    // ── Task erstellen (Schüler und Admin; Lehrer dürfen nicht) ─────────────
    [HttpPost]
    [Authorize(Roles = "Admin,Student")]
    public async Task<IActionResult> Create(int projectId, [FromBody] TaskDto dto)
    {
        // Prüfen: Assignee muss Projektmitglied oder Owner sein
        string? resolvedAssigneeId = null;
        if (!string.IsNullOrEmpty(dto.AssigneeId))
        {
            var isMember = await _db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == dto.AssigneeId);
            var isOwner = await _db.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == dto.AssigneeId);
            if (!isMember && !isOwner)
                return BadRequest(new { error = "Die zugewiesene Person ist kein Mitglied dieses Projekts." });
            resolvedAssigneeId = dto.AssigneeId;
        }

        // Prüfen: Task darf Eltern-Meilenstein nicht überschreiten
        if (dto.ParentId != null)
        {
            var parent = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == dto.ParentId && t.ProjectId == projectId);
            if (parent != null && parent.IsMilestone)
            {
                if (dto.StartDate < parent.StartDate || dto.EndDate > parent.EndDate)
                    return BadRequest(new { error = $"Task muss innerhalb des Meilensteins liegen ({parent.StartDate:dd.MM.yyyy} – {parent.EndDate:dd.MM.yyyy})." });
            }
        }

        var task = new ProjectTask
        {
            Title = dto.Title,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Progress = Math.Clamp(dto.Progress, 0, 100),
            ParentId = dto.ParentId,
            Priority = dto.Priority ?? "Medium",
            Status = dto.Status ?? "Open",
            IsMilestone = dto.IsMilestone ?? false,
            Note = dto.Note,
            ProjectId = projectId,
            AssigneeId = resolvedAssigneeId,
            PlannedDuration = dto.PlannedDuration
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return Ok(new { tid = task.Id });
    }

    // ── Task aktualisieren (Lehrer dürfen nicht) ─────────────────────────────
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Student")]
    public async Task<IActionResult> Update(int projectId, int id, [FromBody] TaskDto dto)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.ProjectId == projectId);
        if (task == null) return NotFound();

        // Prüfen: neuer Assignee muss Projektmitglied oder Owner sein
        if (dto.AssigneeId != null && dto.AssigneeId != "")
        {
            var isMember = await _db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == dto.AssigneeId);
            var isOwner = await _db.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == dto.AssigneeId);
            if (!isMember && !isOwner)
                return BadRequest(new { error = "Die zugewiesene Person ist kein Mitglied dieses Projekts." });
        }

        // Prüfen: Task darf Eltern-Meilenstein nicht überschreiten
        var parentId = dto.ParentId ?? task.ParentId;
        if (parentId != null)
        {
            var parent = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == parentId && t.ProjectId == projectId);
            if (parent != null && parent.IsMilestone)
            {
                if (dto.StartDate < parent.StartDate || dto.EndDate > parent.EndDate)
                    return BadRequest(new { error = $"Task muss innerhalb des Meilensteins liegen ({parent.StartDate:dd.MM.yyyy} – {parent.EndDate:dd.MM.yyyy})." });
            }
        }

        task.Title = dto.Title;
        task.StartDate = dto.StartDate;
        task.EndDate = dto.EndDate;
        task.Progress = Math.Clamp(dto.Progress, 0, 100);
        task.ParentId = dto.ParentId;
        if (dto.Priority != null) task.Priority = dto.Priority;
        if (dto.Status != null) task.Status = dto.Status;
        if (dto.IsMilestone != null) task.IsMilestone = dto.IsMilestone.Value;
        if (dto.Note != null) task.Note = dto.Note;
        if (dto.AssigneeId != null) task.AssigneeId = dto.AssigneeId == "" ? null : dto.AssigneeId;
        if (dto.PlannedDuration != null) task.PlannedDuration = dto.PlannedDuration;

        await _db.SaveChangesAsync();
        return Ok(task);
    }

    // ── Task löschen (Lehrer dürfen nicht) ──────────────────────────────────
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Student")]
    public async Task<IActionResult> Delete(int projectId, int id)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.ProjectId == projectId);
        if (task == null) return NotFound();

        var links = _db.TaskLinks.Where(l => l.Source == id || l.Target == id);
        _db.TaskLinks.RemoveRange(links);
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Link erstellen (Lehrer dürfen nicht) ─────────────────────────────────
    [HttpPost("links")]
    [Authorize(Roles = "Admin,Student")]
    public async Task<IActionResult> CreateLink(int projectId, [FromBody] LinkDto dto)
    {
        var link = new TaskLink
        {
            Source = dto.Source,
            Target = dto.Target,
            Type = dto.Type ?? "0",
            ProjectId = projectId
        };
        _db.TaskLinks.Add(link);
        await _db.SaveChangesAsync();
        return Ok(new { tid = link.Id });
    }

    // ── Link löschen (Lehrer dürfen nicht) ───────────────────────────────────
    [HttpDelete("links/{linkId:int}")]
    [Authorize(Roles = "Admin,Student")]
    public async Task<IActionResult> DeleteLink(int projectId, int linkId)
    {
        var link = await _db.TaskLinks.FirstOrDefaultAsync(l => l.Id == linkId && l.ProjectId == projectId);
        if (link == null) return NotFound();
        _db.TaskLinks.Remove(link);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Kommentare laden ─────────────────────────────────────────────────────
    [HttpGet("{id:int}/comments")]
    public async Task<IActionResult> GetComments(int projectId, int id)
    {
        var comments = await _db.TaskComments
            .Include(c => c.User)
            .Where(c => c.TaskId == id)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Text,
                c.CreatedAt,
                authorName = c.User.FullName,
                authorRole = c.User.Role
            })
            .ToListAsync();
        return Ok(comments);
    }

    // ── Kommentar hinzufügen ─────────────────────────────────────────────────
    [HttpPost("{id:int}/comments")]
    public async Task<IActionResult> AddComment(int projectId, int id, [FromBody] CommentDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var comment = new TaskComment
        {
            Text = dto.Text,
            TaskId = id,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _db.TaskComments.Add(comment);
        await _db.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(userId);
        return Ok(new
        {
            comment.Id,
            comment.Text,
            comment.CreatedAt,
            authorName = user?.FullName,
            authorRole = user?.Role
        });
    }

    // ── Kommentar löschen ────────────────────────────────────────────────────
    [HttpDelete("{id:int}/comments/{commentId:int}")]
    public async Task<IActionResult> DeleteComment(int projectId, int id, int commentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var comment = await _db.TaskComments.FirstOrDefaultAsync(c => c.Id == commentId && c.TaskId == id);
        if (comment == null) return NotFound();
        if (comment.UserId != userId && role is not ("Admin" or "Teacher")) return Forbid();

        _db.TaskComments.Remove(comment);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record TaskDto(
    string Title,
    DateTime StartDate,
    DateTime EndDate,
    int Progress,
    int? ParentId,
    string? AssigneeId = null,
    string? Priority = null,
    string? Status = null,
    bool? IsMilestone = null,
    string? Note = null,
    decimal? PlannedDuration = null
);
public record LinkDto(int Source, int Target, string? Type);
public record CommentDto(string Text);