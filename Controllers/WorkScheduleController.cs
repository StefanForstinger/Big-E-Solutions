using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPlanner.Data;
using ProjectPlanner.Models;
using ProjectPlanner.Services;

namespace ProjectPlanner.Controllers;

/// <summary>
/// Verwaltung von Arbeitszeitplänen (Arbeitstage + Arbeitszeiten).
/// Nur Admin darf Zeitpläne anlegen/bearbeiten/löschen.
/// Alle können lesen.
/// </summary>
[ApiController]
[Route("api/schedules")]
[Authorize]
public class WorkScheduleController : ControllerBase
{
    private readonly AppDbContext _db;
    // ✅ FIX TF-13: Inject PlanningService so schedule changes trigger recalculation
    private readonly PlanningService _planning;

    public WorkScheduleController(AppDbContext db, PlanningService planning)
    {
        _db = db;
        _planning = planning;
    }

    // ── Alle Zeitpläne (global + projektspezifisch) ──────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? projectId = null)
    {
        var query = _db.WorkSchedules.AsQueryable();
        if (projectId.HasValue)
            query = query.Where(ws => ws.ProjectId == projectId || ws.ProjectId == null);

        return Ok(await query.OrderByDescending(ws => ws.IsDefault).ToListAsync());
    }

    // ── Einzelnen Zeitplan abrufen ───────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var schedule = await _db.WorkSchedules.FindAsync(id);
        if (schedule == null) return NotFound();
        return Ok(schedule);
    }

    // ── Zeitplan anlegen (nur Admin) ─────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] WorkScheduleDto dto)
    {
        if (dto is null)
            return BadRequest(new { error = "Keine Daten übermittelt." });

        var validationError = ValidateScheduleDto(dto);
        if (validationError != null) return BadRequest(new { error = validationError });

        // Wenn neuer Zeitplan als Default gesetzt wird, alten Default entfernen
        if (dto.IsDefault)
            await ClearDefaultFlag(dto.ProjectId);

        var schedule = new WorkSchedule
        {
            Name           = dto.Name,
            ProjectId      = dto.ProjectId,
            WorkDaysMask   = dto.WorkDaysMask,
            DailyStartTime = dto.DailyStartTime,
            DailyEndTime   = dto.DailyEndTime,
            DailyHours     = dto.DailyHours,
            IsDefault      = dto.IsDefault
        };

        _db.WorkSchedules.Add(schedule);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = schedule.Id }, schedule);
    }

    // ── Zeitplan bearbeiten (nur Admin) ──────────────────────────────────────
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] WorkScheduleDto dto)
    {
        if (dto is null)
            return BadRequest(new { error = "Keine Daten übermittelt." });

        var validationError = ValidateScheduleDto(dto);
        if (validationError != null) return BadRequest(new { error = validationError });

        var schedule = await _db.WorkSchedules.FindAsync(id);
        if (schedule == null) return NotFound();

        if (dto.IsDefault && !schedule.IsDefault)
            await ClearDefaultFlag(dto.ProjectId);

        schedule.Name           = dto.Name;
        schedule.ProjectId      = dto.ProjectId;
        schedule.WorkDaysMask   = dto.WorkDaysMask;
        schedule.DailyStartTime = dto.DailyStartTime;
        schedule.DailyEndTime   = dto.DailyEndTime;
        schedule.DailyHours     = dto.DailyHours;
        schedule.IsDefault      = dto.IsDefault;

        await _db.SaveChangesAsync();

        // ✅ FIX TF-13: Recalculate all tasks affected by this schedule change
        await RecalculateTasksForScheduleAsync(schedule);

        return Ok(schedule);
    }

    // ── Zeitplan löschen (nur Admin) ─────────────────────────────────────────
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var schedule = await _db.WorkSchedules.FindAsync(id);
        if (schedule == null) return NotFound();

        _db.WorkSchedules.Remove(schedule);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Hilfsmethode: Alle Tasks nach Schedule-Änderung neu berechnen ────────
    // FIX TF-13: Kaskadierung auslösen wenn Arbeitszeit geändert wird
    private async Task RecalculateTasksForScheduleAsync(WorkSchedule schedule)
    {
        List<ProjectTask> tasksToRecalculate;

        if (schedule.ProjectId.HasValue)
        {
            // Projektspezifischer Schedule: alle Tasks dieses Projekts neu berechnen
            tasksToRecalculate = await _db.Tasks
                .Where(t => t.ProjectId == schedule.ProjectId && !t.IsMilestone && t.PlannedDuration != null && t.PlannedDuration > 0)
                .OrderBy(t => t.StartDate)
                .ToListAsync();
        }
        else if (schedule.IsDefault)
        {
            // Globaler Default-Schedule: alle Projekte ohne eigenen projektspezifischen Schedule
            var projectsWithOwnSchedule = await _db.WorkSchedules
                .Where(ws => ws.ProjectId != null)
                .Select(ws => ws.ProjectId!.Value)
                .Distinct()
                .ToListAsync();

            tasksToRecalculate = await _db.Tasks
                .Where(t => !projectsWithOwnSchedule.Contains(t.ProjectId)
                            && !t.IsMilestone
                            && t.PlannedDuration != null
                            && t.PlannedDuration > 0)
                .OrderBy(t => t.StartDate)
                .ToListAsync();
        }
        else
        {
            return; // Nicht-default globaler Schedule: kein Projekt betroffen
        }

        foreach (var task in tasksToRecalculate)
            await _planning.CalculateScheduleAsync(task);
    }

    // ── Validierungshilfe ────────────────────────────────────────────────────
    private static string? ValidateScheduleDto(WorkScheduleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length < 2)
            return "Name muss mindestens 2 Zeichen lang sein.";

        if (dto.WorkDaysMask <= 0 || dto.WorkDaysMask > 127)
            return "Mindestens ein Arbeitstag muss ausgewählt sein (WorkDaysMask 1–127).";

        if (dto.DailyHours <= 0 || dto.DailyHours > 24)
            return "Stunden pro Tag müssen zwischen 0,5 und 24 liegen.";

        // HH:MM Format prüfen
        var timeRegex = new System.Text.RegularExpressions.Regex(@"^([01]\d|2[0-3]):[0-5]\d$");
        if (!timeRegex.IsMatch(dto.DailyStartTime ?? ""))
            return "Arbeitsbeginn muss im Format HH:MM angegeben werden (z.B. 08:00).";
        if (!timeRegex.IsMatch(dto.DailyEndTime ?? ""))
            return "Arbeitsende muss im Format HH:MM angegeben werden (z.B. 17:00).";

        if (string.Compare(dto.DailyStartTime, dto.DailyEndTime, StringComparison.Ordinal) >= 0)
            return "Arbeitsende muss nach dem Arbeitsbeginn liegen.";

        return null; // alles ok
    }

    // ── Hilfsmethode: Default-Flag bei anderen Zeitplänen entfernen ──────────
    private async Task ClearDefaultFlag(int? projectId)
    {
        var defaults = await _db.WorkSchedules
            .Where(ws => ws.IsDefault && ws.ProjectId == projectId)
            .ToListAsync();
        foreach (var ws in defaults)
            ws.IsDefault = false;
        await _db.SaveChangesAsync();
    }
}

public record WorkScheduleDto(
    string  Name,
    int?    ProjectId,
    int     WorkDaysMask,
    string  DailyStartTime,
    string  DailyEndTime,
    decimal DailyHours,
    bool    IsDefault
);
