using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace ProjectPlanner.Models;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>Kürzel des Mitarbeiters (z.B. "MAY" für Mayr)</summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>Stundensatz in Euro</summary>
    public decimal HourlyRate { get; set; } = 0;

    /// <summary>Student | Teacher | Admin</summary>
    public string Role { get; set; } = "Student";

    /// <summary>true = Benutzer muss beim nächsten Login das Passwort ändern</summary>
    public bool MustChangePassword { get; set; } = true;

    /// <summary>true = Datenschutzbestimmungen wurden akzeptiert</summary>
    public bool PrivacyAccepted { get; set; } = false;

    [JsonIgnore]
    public ICollection<Project> Projects { get; set; } = new List<Project>();

    [JsonIgnore]
    public ICollection<PrivacyConsent> PrivacyConsents { get; set; } = new List<PrivacyConsent>();

    [JsonIgnore]
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}
