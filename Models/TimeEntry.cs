using System.Text.Json.Serialization;

namespace ProjectPlanner.Models;

/// <summary>
/// Gestempelter Arbeitszeitblock eines Benutzers an einer Aufgabe.
/// EndTime = null bedeutet: die Stempeluhr läuft noch.
/// </summary>
public class TimeEntry
{
    public int    Id        { get; set; }
    public string UserId    { get; set; } = string.Empty;
    public int    TaskId    { get; set; }

    /// <summary>Redundant gespeichert für einfaches Reporting ohne JOIN über Tasks</summary>
    public int    ProjectId { get; set; }

    /// <summary>Stempeluhr: Beginn (UTC)</summary>
    public DateTime StartTime { get; set; }

    /// <summary>Stempeluhr: Ende (UTC). null = läuft noch</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>Berechnete Dauer in Stunden. Wird beim Ausstempeln gesetzt.</summary>
    public decimal? DurationHours { get; set; }

    /// <summary>Optionale Tätigkeitsbeschreibung</summary>
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>true = manuell eingetragen (nicht über Stempeluhr)</summary>
    public bool IsManual { get; set; } = false;

    [JsonIgnore] public AppUser     User    { get; set; } = null!;
    [JsonIgnore] public ProjectTask Task    { get; set; } = null!;
    [JsonIgnore] public Project     Project { get; set; } = null!;
}
