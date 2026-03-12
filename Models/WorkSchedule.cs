using System.Text.Json.Serialization;

namespace ProjectPlanner.Models;

/// <summary>
/// Definition von Arbeitstagen und Arbeitszeiten.
/// Kann global (ProjectId = null) oder projektspezifisch sein.
/// WorkDaysMask: Bitmaske – Mo=2, Di=4, Mi=8, Do=16, Fr=32, Sa=64, So=1
/// Standard Mo–Fr = 62
/// </summary>
public class WorkSchedule
{
    public int    Id   { get; set; }
    public string Name { get; set; } = "Standard-Woche";

    /// <summary>null = global, sonst projektspezifisch</summary>
    public int? ProjectId { get; set; }

    /// <summary>Bitmaske der Arbeitstage. Mo–Fr = 62</summary>
    public int WorkDaysMask { get; set; } = 62;

    /// <summary>Tagesstart im Format HH:MM</summary>
    public string DailyStartTime { get; set; } = "08:00";

    /// <summary>Tagesende im Format HH:MM</summary>
    public string DailyEndTime { get; set; } = "17:00";

    /// <summary>Arbeitsstunden pro Tag</summary>
    public decimal DailyHours { get; set; } = 8;

    /// <summary>true = Standard-Zeitplan</summary>
    public bool IsDefault { get; set; } = false;

    [JsonIgnore] public Project? Project { get; set; }
}
