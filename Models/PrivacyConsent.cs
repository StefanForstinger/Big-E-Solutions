using System.Text.Json.Serialization;

namespace ProjectPlanner.Models;

/// <summary>DSGVO-Zustimmung eines Benutzers mit Zeitstempel und IP-Adresse</summary>
public class PrivacyConsent
{
    public int    Id     { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>Zeitpunkt der Zustimmung (UTC)</summary>
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;

    /// <summary>IP-Adresse des Clients (IPv4 oder IPv6)</summary>
    public string? IpAddress { get; set; }

    /// <summary>Version der Datenschutzbestimmungen</summary>
    public string Version { get; set; } = "1.0";

    /// <summary>true = zugestimmt (false = widerrufen, für künftige Erweiterung)</summary>
    public bool Accepted { get; set; } = true;

    [JsonIgnore] public AppUser User { get; set; } = null!;
}
