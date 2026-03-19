using System.Text.Json.Serialization;

namespace ProjectPlanner.Models;

public class ProjectTask
{
    public int      Id        { get; set; }
    public string   Title     { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate   { get; set; }

    /// <summary>Geplante Dauer in Stunden (manuell gepflegt)</summary>
    public decimal? PlannedDuration { get; set; }

    /// <summary>Tatsächliche Dauer in Stunden – Summe aus TIME_ENTRIES</summary>
    public decimal ActualDuration { get; set; } = 0;

    /// <summary>Arbeitsanteil in % basierend auf gestempelten Stunden</summary>
    public decimal WorkSharePercent { get; set; } = 0;

    /// <summary>0–100 Prozent</summary>
    public int Progress { get; set; } = 0;

    /// <summary>Übergeordnete Aufgabe für Gantt-Hierarchie</summary>
    public int? ParentId { get; set; }

    /// <summary>Low | Medium | High</summary>
    public string Priority { get; set; } = "Medium";

    /// <summary>Open | InProgress | Done | Blocked</summary>
    public string Status { get; set; } = "Open";

    /// <summary>Ist diese Task ein Meilenstein?</summary>
    public bool IsMilestone { get; set; } = false;

    /// <summary>Optionale Notiz / Kommentar</summary>
    public string? Note { get; set; }

    /// <summary>DEPRECATED: Wird durch TaskAssignments ersetzt. Bleibt für Abwärtskompatibilität.</summary>
    [Obsolete("Use TaskAssignments instead for multiple assignees with percentages")]
    public string? AssigneeId { get; set; }
    [JsonIgnore] 
    [Obsolete("Use TaskAssignments instead")]
    public AppUser? Assignee { get; set; }

    public int     ProjectId { get; set; }
    [JsonIgnore] public Project Project { get; set; } = null!;

    [JsonIgnore] public ICollection<TaskLink>       LinksFrom      { get; set; } = new List<TaskLink>();
    [JsonIgnore] public ICollection<TaskLink>       LinksTo        { get; set; } = new List<TaskLink>();
    [JsonIgnore] public ICollection<TaskComment>    Comments       { get; set; } = new List<TaskComment>();
    [JsonIgnore] public ICollection<TimeEntry>      TimeEntries    { get; set; } = new List<TimeEntry>();
    [JsonIgnore] public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
}
