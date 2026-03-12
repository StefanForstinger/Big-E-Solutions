using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPlanner.Data;
using ProjectPlanner.Models;

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

    public WorkScheduleController(AppDbContext db) => _db = db;

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
