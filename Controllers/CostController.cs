using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPlanner.Data;
using System.Security.Claims;

namespace ProjectPlanner.Controllers;

/// <summary>
/// Kostenplanung und Kostenauswertung basierend auf Stundensätzen
/// KANN-Anforderungen aus Lastenheft
/// </summary>
[ApiController]
[Route("api/costs")]
[Authorize]
public class CostController : ControllerBase
{
    private readonly AppDbContext _db;

    public CostController(AppDbContext db) => _db = db;

    // ── Kostenplanung auf Grund der geplanten Stunden ────────────────────────
    [HttpGet("planned/{projectId:int}")]
    public async Task<IActionResult> GetPlannedCosts(int projectId)
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

        // Hole alle Tasks mit ihren Zuweisungen
        var tasks = await _db.Tasks
            .Include(t => t.TaskAssignments)
            .ThenInclude(ta => ta.User)
            .Where(t => t.ProjectId == projectId && t.PlannedDuration.HasValue)
            .ToListAsync();

        var costBreakdown = new List<object>();
        decimal totalPlannedCost = 0;

        foreach (var task in tasks)
        {
            if (!task.PlannedDuration.HasValue) continue;

            var taskCost = 0m;
            var assignmentCosts = new List<object>();

            foreach (var assignment in task.TaskAssignments)
            {
                // Berechne anteilige Stunden basierend auf Prozentsatz
                var assignedHours = task.PlannedDuration.Value * (assignment.Percentage / 100m);
                var cost = assignedHours * assignment.User.HourlyRate;
                
                taskCost += cost;

                assignmentCosts.Add(new
                {
                    userId = assignment.UserId,
                    userName = assignment.User.FullName,
                    shortName = assignment.User.ShortName,
                    hourlyRate = assignment.User.HourlyRate,
                    percentage = assignment.Percentage,
                    hours = assignedHours,
                    cost = Math.Round(cost, 2)
                });
            }

            totalPlannedCost += taskCost;

            costBreakdown.Add(new
            {
                taskId = task.Id,
                taskTitle = task.Title,
                plannedDuration = task.PlannedDuration.Value,
                taskCost = Math.Round(taskCost, 2),
                assignments = assignmentCosts
            });
        }

        // Kosten pro Mitarbeiter aggregieren
        var memberCosts = tasks
            .SelectMany(t => t.TaskAssignments.Select(ta => new
            {
                UserId = ta.UserId,
                UserName = ta.User.FullName,
                ShortName = ta.User.ShortName,
                HourlyRate = ta.User.HourlyRate,
                Hours = t.PlannedDuration.HasValue ? t.PlannedDuration.Value * (ta.Percentage / 100m) : 0
            }))
            .GroupBy(x => new { x.UserId, x.UserName, x.ShortName, x.HourlyRate })
            .Select(g => new
            {
                userId = g.Key.UserId,
                userName = g.Key.UserName,
                shortName = g.Key.ShortName,
                hourlyRate = g.Key.HourlyRate,
                totalHours = g.Sum(x => x.Hours),
                totalCost = Math.Round(g.Sum(x => x.Hours * x.HourlyRate), 2)
            })
            .OrderByDescending(x => x.totalCost)
            .ToList();

        return Ok(new
        {
            projectId,
            totalPlannedCost = Math.Round(totalPlannedCost, 2),
            totalPlannedHours = tasks.Sum(t => t.PlannedDuration ?? 0),
            costBreakdown,
            memberCosts
        });
    }

    // ── Kostenauswertung auf Grund der tatsächlichen Stunden ─────────────────
    [HttpGet("actual/{projectId:int}")]
    public async Task<IActionResult> GetActualCosts(int projectId)
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

        // Hole alle TimeEntries für das Projekt
        var timeEntries = await _db.TimeEntries
            .Include(te => te.User)
            .Include(te => te.Task)
            .Where(te => te.ProjectId == projectId && te.DurationHours.HasValue)
            .ToListAsync();

        var totalActualCost = timeEntries.Sum(te => 
            (te.DurationHours ?? 0) * te.User.HourlyRate);

        // Kosten pro Aufgabe
        var taskCosts = timeEntries
            .GroupBy(te => new { te.TaskId, te.Task.Title })
            .Select(g => new
            {
                taskId = g.Key.TaskId,
                taskTitle = g.Key.Title,
                totalHours = g.Sum(te => te.DurationHours ?? 0),
                totalCost = Math.Round(g.Sum(te => (te.DurationHours ?? 0) * te.User.HourlyRate), 2),
                entries = g.GroupBy(te => new { te.UserId, te.User.FullName, te.User.ShortName, te.User.HourlyRate })
                    .Select(ug => new
                    {
                        userId = ug.Key.UserId,
                        userName = ug.Key.FullName,
                        shortName = ug.Key.ShortName,
                        hourlyRate = ug.Key.HourlyRate,
                        hours = ug.Sum(te => te.DurationHours ?? 0),
                        cost = Math.Round(ug.Sum(te => (te.DurationHours ?? 0) * ug.Key.HourlyRate), 2)
                    })
                    .ToList()
            })
            .OrderByDescending(x => x.totalCost)
            .ToList();

        // Kosten pro Mitarbeiter
        var memberCosts = timeEntries
            .GroupBy(te => new { te.UserId, te.User.FullName, te.User.ShortName, te.User.HourlyRate })
            .Select(g => new
            {
                userId = g.Key.UserId,
                userName = g.Key.FullName,
                shortName = g.Key.ShortName,
                hourlyRate = g.Key.HourlyRate,
                totalHours = g.Sum(te => te.DurationHours ?? 0),
                totalCost = Math.Round(g.Sum(te => (te.DurationHours ?? 0) * g.Key.HourlyRate), 2)
            })
            .OrderByDescending(x => x.totalCost)
            .ToList();

        return Ok(new
        {
            projectId,
            totalActualCost = Math.Round(totalActualCost, 2),
            totalActualHours = timeEntries.Sum(te => te.DurationHours ?? 0),
            taskCosts,
            memberCosts
        });
    }

    // ── Kostenvergleich: Geplant vs. Tatsächlich ─────────────────────────────
    [HttpGet("comparison/{projectId:int}")]
    public async Task<IActionResult> GetCostComparison(int projectId)
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

        // Geplante Kosten
        var tasks = await _db.Tasks
            .Include(t => t.TaskAssignments)
            .ThenInclude(ta => ta.User)
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        var plannedCost = tasks.Sum(task =>
            task.TaskAssignments.Sum(ta =>
                (task.PlannedDuration ?? 0) * (ta.Percentage / 100m) * ta.User.HourlyRate));

        var plannedHours = tasks.Sum(t => t.PlannedDuration ?? 0);

        // Tatsächliche Kosten
        var timeEntries = await _db.TimeEntries
            .Include(te => te.User)
            .Where(te => te.ProjectId == projectId && te.DurationHours.HasValue)
            .ToListAsync();

        var actualCost = timeEntries.Sum(te =>
            (te.DurationHours ?? 0) * te.User.HourlyRate);

        var actualHours = timeEntries.Sum(te => te.DurationHours ?? 0);

        // Abweichungen
        var costVariance = actualCost - plannedCost;
        var costVariancePercent = plannedCost > 0 
            ? Math.Round((costVariance / plannedCost) * 100, 2) 
            : 0;

        var hoursVariance = actualHours - plannedHours;
        var hoursVariancePercent = plannedHours > 0
            ? Math.Round((hoursVariance / plannedHours) * 100, 2)
            : 0;

        return Ok(new
        {
            projectId,
            planned = new
            {
                cost = Math.Round(plannedCost, 2),
                hours = Math.Round(plannedHours, 2)
            },
            actual = new
            {
                cost = Math.Round(actualCost, 2),
                hours = Math.Round(actualHours, 2)
            },
            variance = new
            {
                cost = Math.Round(costVariance, 2),
                costPercent = costVariancePercent,
                hours = Math.Round(hoursVariance, 2),
                hoursPercent = hoursVariancePercent
            },
            status = costVariance > 0 ? "over_budget" 
                   : costVariance < 0 ? "under_budget" 
                   : "on_budget"
        });
    }

    // ── Auslastung der Projektmitglieder in Prozent ──────────────────────────
    [HttpGet("utilization/{projectId:int}")]
    public async Task<IActionResult> GetUtilization(int projectId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
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

        // Hole Projektmitglieder
        var project = await _db.Projects
            .Include(p => p.Members)
            .ThenInclude(m => m.User)
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null) return NotFound(new { error = "Projekt nicht gefunden" });

        // Alle Mitglieder (Owner + Members)
        var allMembers = new List<string> { project.OwnerId };
        allMembers.AddRange(project.Members.Select(m => m.UserId));

        // Lade User-Details
        var users = await _db.Users
            .Where(u => allMembers.Contains(u.Id))
            .ToListAsync();

        // TimeEntries im Zeitraum
        var query = _db.TimeEntries
            .Include(te => te.User)
            .Where(te => te.ProjectId == projectId && te.DurationHours.HasValue);

        if (from.HasValue) query = query.Where(te => te.StartTime >= from.Value);
        if (to.HasValue) query = query.Where(te => te.StartTime <= to.Value);

        var timeEntries = await query.ToListAsync();

        // Berechne Auslastung pro Mitglied
        var totalProjectHours = timeEntries.Sum(te => te.DurationHours ?? 0);

        var utilization = users.Select(user =>
        {
            var userHours = timeEntries
                .Where(te => te.UserId == user.Id)
                .Sum(te => te.DurationHours ?? 0);

            var utilizationPercent = totalProjectHours > 0
                ? Math.Round((userHours / totalProjectHours) * 100, 2)
                : 0;

            return new
            {
                userId = user.Id,
                userName = user.FullName,
                shortName = user.ShortName,
                hourlyRate = user.HourlyRate,
                totalHours = Math.Round(userHours, 2),
                utilizationPercent,
                cost = Math.Round(userHours * user.HourlyRate, 2)
            };
        })
        .OrderByDescending(x => x.utilizationPercent)
        .ToList();

        return Ok(new
        {
            projectId,
            period = new
            {
                from = from?.ToString("yyyy-MM-dd"),
                to = to?.ToString("yyyy-MM-dd")
            },
            totalProjectHours = Math.Round(totalProjectHours, 2),
            utilization
        });
    }
}
