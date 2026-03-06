using System.Text.Json.Serialization;

namespace ProjectPlanner.Models;

/// <summary>Abhängigkeit zwischen zwei Tasks (DHTMLX-Gantt Links)</summary>
public class TaskLink
{
    public int    Id     { get; set; }
    public int    Source { get; set; }   // Von Task
    public int    Target { get; set; }   // Zu Task
    /// <summary>0=FS, 1=SS, 2=FF, 3=SF</summary>
    public string Type   { get; set; } = "0";

    public int ProjectId { get; set; }

    [JsonIgnore] public ProjectTask SourceTask  { get; set; } = null!;
    [JsonIgnore] public ProjectTask TargetTask  { get; set; } = null!;
    [JsonIgnore] public Project     Project     { get; set; } = null!;
}
