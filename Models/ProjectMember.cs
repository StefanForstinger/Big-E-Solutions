using System.Text.Json.Serialization;

namespace ProjectPlanner.Models;

/// <summary>Verknüpfungstabelle: Schüler ↔ Projekt</summary>
public class ProjectMember
{
    public int    ProjectId { get; set; }
    public string UserId    { get; set; } = string.Empty;

    [JsonIgnore] public Project Project { get; set; } = null!;
    [JsonIgnore] public AppUser User    { get; set; } = null!;
}
