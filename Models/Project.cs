using System.Text.Json.Serialization;

namespace ProjectPlanner.Models;

public class Project
{
    public int      Id          { get; set; }
    public string   Name        { get; set; } = string.Empty;
    public string   Description { get; set; } = string.Empty;
    public DateTime StartDate   { get; set; }
    public DateTime EndDate     { get; set; }

    /// <summary>Hex-Farbe z.B. #2D9CDB</summary>
    public string Color { get; set; } = "#2D9CDB";

    public string OwnerId { get; set; } = string.Empty;
    [JsonIgnore] public AppUser Owner { get; set; } = null!;

    public ICollection<ProjectTask>   Tasks         { get; set; } = new List<ProjectTask>();
    [JsonIgnore] public ICollection<ProjectMember> Members       { get; set; } = new List<ProjectMember>();
    [JsonIgnore] public ICollection<TaskLink>      Links         { get; set; } = new List<TaskLink>();
    [JsonIgnore] public ICollection<WorkSchedule>  WorkSchedules { get; set; } = new List<WorkSchedule>();
    [JsonIgnore] public ICollection<TimeEntry>     TimeEntries   { get; set; } = new List<TimeEntry>();
}
