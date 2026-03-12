using System.Text.Json.Serialization;

namespace ProjectPlanner.Models;

/// <summary>Verknüpfungstabelle: Schüler ↔ Projekt</summary>
public class ProjectMember
{
    public int    ProjectId { get; set; }
    public string UserId    { get; set; } = string.Empty;

    /// <summary>Zeitpunkt der Zuweisung</summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore] public Project Project { get; set; } = null!;
    [JsonIgnore] public AppUser User    { get; set; } = null!;
}
