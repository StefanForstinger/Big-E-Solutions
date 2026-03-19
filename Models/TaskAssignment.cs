using System.Text.Json.Serialization;

namespace ProjectPlanner.Models;

/// <summary>
/// Zuweisung eines Mitarbeiters zu einer Aufgabe mit Prozentangabe.
/// Ermöglicht Mehrfachzuweisung wie: Mayr[50%], Schuster[50%]
/// </summary>
public class TaskAssignment
{
    public int Id { get; set; }

    public int TaskId { get; set; }
    [JsonIgnore] public ProjectTask Task { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    [JsonIgnore] public AppUser User { get; set; } = null!;

    /// <summary>Arbeitsanteil in Prozent (0-100)</summary>
    public decimal Percentage { get; set; } = 100;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
