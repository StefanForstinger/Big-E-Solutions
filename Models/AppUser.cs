using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace ProjectPlanner.Models;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>Student | Teacher | Admin</summary>
    public string Role { get; set; } = "Student";

    [JsonIgnore]
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
