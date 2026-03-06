using System.Text.Json.Serialization;

namespace ProjectPlanner.Models;

public class TaskComment
{
    public int      Id        { get; set; }
    public string   Text      { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int    TaskId { get; set; }
    public string UserId { get; set; } = string.Empty;

    [JsonIgnore] public ProjectTask Task { get; set; } = null!;
    [JsonIgnore] public AppUser     User { get; set; } = null!;

    // Für Serialisierung
    public string? AuthorName { get; set; }
}
