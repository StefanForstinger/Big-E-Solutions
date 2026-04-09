using Microsoft.EntityFrameworkCore;
using ProjectPlanner.Data;
using ProjectPlanner.Models;

namespace ProjectPlanner.Services;

/// <summary>
/// Vorwärtsplanung: Berechnet Enddatum basierend auf StartDate, PlannedDuration,
/// WorkSchedule (Arbeitstage + Stunden pro Tag) und Vorgänger-Abhängigkeiten (TaskLinks).
/// </summary>
public class PlanningService
{
    private readonly AppDbContext _db;

    public PlanningService(AppDbContext db) => _db = db;

    /// <summary>
    /// Berechnet das Enddatum eines Tasks und kaskadiert auf alle Nachfolger.
    /// </summary>
    public async Task CalculateScheduleAsync(ProjectTask task)
    {
        // Meilensteine behalten manuelles Start-/Enddatum
        if (task.IsMilestone) return;

        // Ohne PlannedDuration kann nichts berechnet werden
        if (task.PlannedDuration == null || task.PlannedDuration <= 0) return;

        // WorkSchedule laden (projektspezifisch oder global default)
        var schedule = await GetWorkScheduleAsync(task.ProjectId);

        // Vorgänger prüfen (Finish-to-Start Links, Type "0")
        var predecessorLinks = await _db.TaskLinks
            .Include(l => l.SourceTask)
            .Where(l => l.Target == task.Id && l.Type == "0")
            .ToListAsync();

        // Effektives Startdatum: nach dem spätesten Vorgänger-Ende
        var effectiveStart = task.StartDate;
        foreach (var link in predecessorLinks)
        {
            if (link.SourceTask.EndDate > effectiveStart)
                effectiveStart = link.SourceTask.EndDate;
        }

        // Startdatum auf nächsten Arbeitstag setzen falls nötig
        effectiveStart = NextWorkDay(effectiveStart, schedule);
        task.StartDate = effectiveStart;

        // Enddatum berechnen
        task.EndDate = CalculateEndDate(effectiveStart, task.PlannedDuration.Value, schedule);

        // Meilenstein-Grenzen prüfen
        if (task.ParentId != null)
        {
            var parent = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == task.ParentId);
            if (parent != null && parent.IsMilestone)
            {
                // Task darf nicht über Meilenstein hinausgehen
                if (task.EndDate > parent.EndDate)
                    task.EndDate = parent.EndDate;
                if (task.StartDate < parent.StartDate)
                    task.StartDate = parent.StartDate;
            }
        }

        await _db.SaveChangesAsync();

        // Nachfolger kaskadieren
        await CascadeSuccessorsAsync(task);
    }

    /// <summary>
    /// Kaskadiert die Planung auf alle Nachfolger-Tasks (rekursiv).
    /// </summary>
    private async Task CascadeSuccessorsAsync(ProjectTask task)
    {
        var successorLinks = await _db.TaskLinks
            .Where(l => l.Source == task.Id && l.Type == "0")
            .Select(l => l.Target)
            .ToListAsync();

        foreach (var targetId in successorLinks)
        {
            var successor = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == targetId);
            if (successor != null && !successor.IsMilestone)
            {
                await CalculateScheduleAsync(successor);
            }
        }
    }

    /// <summary>
    /// Berechnet das Enddatum basierend auf Startdatum, Dauer (Stunden) und Arbeitsplan.
    /// Überspringt Nicht-Arbeitstage gemäß WorkDaysMask.
    /// </summary>
    private DateTime CalculateEndDate(DateTime start, decimal durationHours, WorkSchedule schedule)
    {
        var dailyHours = schedule.DailyHours > 0 ? schedule.DailyHours : 8m;
        var remainingHours = durationHours;
        var current = start;

        // Sonderfall: Dauer passt in einen Tag
        if (remainingHours <= dailyHours)
        {
            return current;
        }

        while (remainingHours > 0)
        {
            if (IsWorkDay(current, schedule))
            {
                remainingHours -= dailyHours;
                if (remainingHours <= 0)
                    return current;
            }
            current = current.AddDays(1);

            // Nächsten Arbeitstag finden
            current = NextWorkDay(current, schedule);
        }

        return current;
    }

    /// <summary>
    /// Prüft ob ein Datum ein Arbeitstag ist (basierend auf WorkDaysMask).
    /// Bitmaske: So=1, Mo=2, Di=4, Mi=8, Do=16, Fr=32, Sa=64
    /// </summary>
    private bool IsWorkDay(DateTime date, WorkSchedule schedule)
    {
        int dayBit = date.DayOfWeek switch
        {
            DayOfWeek.Sunday => 1,
            DayOfWeek.Monday => 2,
            DayOfWeek.Tuesday => 4,
            DayOfWeek.Wednesday => 8,
            DayOfWeek.Thursday => 16,
            DayOfWeek.Friday => 32,
            DayOfWeek.Saturday => 64,
            _ => 0
        };
        return (schedule.WorkDaysMask & dayBit) != 0;
    }

    /// <summary>
    /// Gibt das Datum selbst zurück wenn es ein Arbeitstag ist,
    /// sonst den nächsten Arbeitstag.
    /// </summary>
    private DateTime NextWorkDay(DateTime date, WorkSchedule schedule)
    {
        // Max 7 Iterationen (eine volle Woche)
        for (int i = 0; i < 7; i++)
        {
            if (IsWorkDay(date, schedule))
                return date;
            date = date.AddDays(1);
        }
        // Fallback: wenn kein Arbeitstag in der Maske → Datum zurückgeben
        return date;
    }

    /// <summary>
    /// Lädt den passenden WorkSchedule: projektspezifisch (default) oder globaler Default.
    /// </summary>
    private async Task<WorkSchedule> GetWorkScheduleAsync(int projectId)
    {
        // Zuerst projektspezifischer Default
        var schedule = await _db.WorkSchedules
            .Where(ws => ws.ProjectId == projectId && ws.IsDefault)
            .FirstOrDefaultAsync();

        // Dann irgendein projektspezifischer
        schedule ??= await _db.WorkSchedules
            .Where(ws => ws.ProjectId == projectId)
            .FirstOrDefaultAsync();

        // Dann globaler Default
        schedule ??= await _db.WorkSchedules
            .Where(ws => ws.ProjectId == null && ws.IsDefault)
            .FirstOrDefaultAsync();

        // Dann irgendein globaler
        schedule ??= await _db.WorkSchedules
            .Where(ws => ws.ProjectId == null)
            .FirstOrDefaultAsync();

        // Fallback: Standard Mo-Fr, 8h
        return schedule ?? new WorkSchedule
        {
            Name = "Fallback",
            WorkDaysMask = 62, // Mo-Fr
            DailyHours = 8,
            DailyStartTime = "08:00",
            DailyEndTime = "17:00"
        };
    }
}