using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectPlanner.Data;
using ProjectPlanner.Models;
using ProjectPlanner.Services;
using Xunit;

namespace ProjectPlanner.Tests;

// ── Test-Hilfsmethoden ────────────────────────────────────────────────────────

public static class TestDb
{
    /// <summary>
    /// Erstellt einen isolierten In-Memory-AppDbContext für jeden Test.
    /// Der Name wird als eindeutiger DB-Key verwendet, damit Tests sich nicht gegenseitig beeinflussen.
    /// </summary>
    public static AppDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>Seed: legt einen WorkSchedule an und gibt dessen ID zurück.</summary>
    public static async Task<WorkSchedule> SeedScheduleAsync(
        AppDbContext db,
        int workDaysMask = 62,   // Mo–Fr
        decimal dailyHours = 8m,
        bool isDefault = true,
        int? projectId = null)
    {
        var schedule = new WorkSchedule
        {
            Name           = "Test-Schedule",
            WorkDaysMask   = workDaysMask,
            DailyHours     = dailyHours,
            DailyStartTime = "08:00",
            DailyEndTime   = "17:00",
            IsDefault      = isDefault,
            ProjectId      = projectId
        };
        db.WorkSchedules.Add(schedule);
        await db.SaveChangesAsync();
        return schedule;
    }

    /// <summary>Seed: legt ein Projekt an.</summary>
    public static async Task<Project> SeedProjectAsync(AppDbContext db)
    {
        // Identity braucht einen AppUser als Owner – wir verwenden einen Stub
        var user = new AppUser
        {
            Id           = "test-owner",
            UserName     = "test@test.at",
            Email        = "test@test.at",
            FullName     = "Test Owner",
            ShortName    = "OWN",
            NormalizedEmail    = "TEST@TEST.AT",
            NormalizedUserName = "TEST@TEST.AT",
        };
        db.Users.Add(user);

        var project = new Project
        {
            Name      = "Test-Projekt",
            OwnerId   = user.Id,
            StartDate = new DateTime(2025, 1, 6), // Montag
            EndDate   = new DateTime(2025, 12, 31),
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    /// <summary>Seed: legt einen ProjectTask an.</summary>
    public static async Task<ProjectTask> SeedTaskAsync(
        AppDbContext db,
        int projectId,
        DateTime startDate,
        decimal? plannedDuration,
        bool isMilestone = false)
    {
        var task = new ProjectTask
        {
            Title           = "Test-Aufgabe",
            StartDate       = startDate,
            EndDate         = startDate,
            PlannedDuration = plannedDuration,
            IsMilestone     = isMilestone,
            ProjectId       = projectId,
            Status          = "Open",
            Priority        = "Medium"
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task;
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public class PlanningServiceTests
{
    // ────────────────────────────────────────────────────────────────────────
    // Gruppe 1: Grundlegende Enddatum-Berechnung
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Task_8h_StartingMonday_EndsOnSameMonday()
    {
        // Arrange
        var db = TestDb.Create(nameof(Task_8h_StartingMonday_EndsOnSameMonday));
        await TestDb.SeedScheduleAsync(db, workDaysMask: 62, dailyHours: 8m);
        var project = await TestDb.SeedProjectAsync(db);
        // Montag, 06.01.2025
        var task = await TestDb.SeedTaskAsync(db, project.Id, new DateTime(2025, 1, 6), 8m);
        var sut = new PlanningService(db);

        // Act
        await sut.CalculateScheduleAsync(task);
        var result = await db.Tasks.FindAsync(task.Id);

        // Assert: 8h = 1 Arbeitstag → Ende = Montag
        Assert.Equal(new DateTime(2025, 1, 6), result!.EndDate);
    }

    [Fact]
    public async Task Task_16h_StartingMonday_EndsOnTuesday()
    {
        var db = TestDb.Create(nameof(Task_16h_StartingMonday_EndsOnTuesday));
        await TestDb.SeedScheduleAsync(db, workDaysMask: 62, dailyHours: 8m);
        var project = await TestDb.SeedProjectAsync(db);
        var task = await TestDb.SeedTaskAsync(db, project.Id, new DateTime(2025, 1, 6), 16m);
        var sut = new PlanningService(db);

        await sut.CalculateScheduleAsync(task);
        var result = await db.Tasks.FindAsync(task.Id);

        // 16h / 8h pro Tag = 2 Tage → Mo + Di
        Assert.Equal(new DateTime(2025, 1, 7), result!.EndDate);
    }

    [Fact]
    public async Task Task_40h_StartingMonday_EndsOnFriday()
    {
        var db = TestDb.Create(nameof(Task_40h_StartingMonday_EndsOnFriday));
        await TestDb.SeedScheduleAsync(db, workDaysMask: 62, dailyHours: 8m);
        var project = await TestDb.SeedProjectAsync(db);
        var task = await TestDb.SeedTaskAsync(db, project.Id, new DateTime(2025, 1, 6), 40m);
        var sut = new PlanningService(db);

        await sut.CalculateScheduleAsync(task);
        var result = await db.Tasks.FindAsync(task.Id);

        // 40h / 8h = 5 Tage → Mo bis Fr
        Assert.Equal(new DateTime(2025, 1, 10), result!.EndDate);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Gruppe 2: Wochenenden werden übersprungen
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Task_16h_StartingFriday_SkipsWeekend_EndsOnMonday()
    {
        var db = TestDb.Create(nameof(Task_16h_StartingFriday_SkipsWeekend_EndsOnMonday));
        await TestDb.SeedScheduleAsync(db, workDaysMask: 62, dailyHours: 8m);
        var project = await TestDb.SeedProjectAsync(db);
        // Freitag 10.01.2025
        var task = await TestDb.SeedTaskAsync(db, project.Id, new DateTime(2025, 1, 10), 16m);
        var sut = new PlanningService(db);

        await sut.CalculateScheduleAsync(task);
        var result = await db.Tasks.FindAsync(task.Id);

        // Fr (8h) + Wochenende überspringen + Mo (8h) → Ende Montag 13.01
        Assert.Equal(new DateTime(2025, 1, 13), result!.EndDate);
    }

    [Fact]
    public async Task Task_StartingOnSaturday_ShiftedToMonday()
    {
        // Startdatum liegt auf einem Wochenende → soll auf nächsten Arbeitstag verschoben werden
        var db = TestDb.Create(nameof(Task_StartingOnSaturday_ShiftedToMonday));
        await TestDb.SeedScheduleAsync(db, workDaysMask: 62, dailyHours: 8m);
        var project = await TestDb.SeedProjectAsync(db);
        // Samstag 11.01.2025
        var task = await TestDb.SeedTaskAsync(db, project.Id, new DateTime(2025, 1, 11), 8m);
        var sut = new PlanningService(db);

        await sut.CalculateScheduleAsync(task);
        var result = await db.Tasks.FindAsync(task.Id);

        // Startdatum auf Montag verschoben, 8h = 1 Tag → Start + Ende = Montag
        Assert.Equal(new DateTime(2025, 1, 13), result!.StartDate);
        Assert.Equal(new DateTime(2025, 1, 13), result!.EndDate);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Gruppe 3: Benutzerdefinierte Arbeitszeitpläne (Bitmask-Varianten)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Task_With_MoWedFri_Schedule_SkipsTueThu()
    {
        // Mo=2, Mi=8, Fr=32 → Maske = 42
        var db = TestDb.Create(nameof(Task_With_MoWedFri_Schedule_SkipsTueThu));
        await TestDb.SeedScheduleAsync(db, workDaysMask: 42, dailyHours: 8m);
        var project = await TestDb.SeedProjectAsync(db);
        // Montag 06.01.2025
        var task = await TestDb.SeedTaskAsync(db, project.Id, new DateTime(2025, 1, 6), 16m);
        var sut = new PlanningService(db);

        await sut.CalculateScheduleAsync(task);
        var result = await db.Tasks.FindAsync(task.Id);

        // Mo (8h) → Di überspringen → Mi (8h) → Ende = Mittwoch 08.01
        Assert.Equal(new DateTime(2025, 1, 8), result!.EndDate);
    }

    [Fact]
    public async Task Task_With_6h_PerDay_Schedule_CalculatesCorrectly()
    {
        var db = TestDb.Create(nameof(Task_With_6h_PerDay_Schedule_CalculatesCorrectly));
        await TestDb.SeedScheduleAsync(db, workDaysMask: 62, dailyHours: 6m);
        var project = await TestDb.SeedProjectAsync(db);
        // Montag 06.01.2025, 18h Aufwand
        var task = await TestDb.SeedTaskAsync(db, project.Id, new DateTime(2025, 1, 6), 18m);
        var sut = new PlanningService(db);

        await sut.CalculateScheduleAsync(task);
        var result = await db.Tasks.FindAsync(task.Id);

        // 18h / 6h = 3 Tage → Mo, Di, Mi → Ende Mittwoch 08.01
        Assert.Equal(new DateTime(2025, 1, 8), result!.EndDate);
    }

    [Fact]
    public async Task Task_With_SatSun_WorkSchedule_WeekendIsWorkDay()
    {
        // Sa=64, So=1 → Wochenend-Betrieb, Maske = 65
        var db = TestDb.Create(nameof(Task_With_SatSun_WorkSchedule_WeekendIsWorkDay));
        await TestDb.SeedScheduleAsync(db, workDaysMask: 65, dailyHours: 8m);
        var project = await TestDb.SeedProjectAsync(db);
        // Samstag 11.01.2025
        var task = await TestDb.SeedTaskAsync(db, project.Id, new DateTime(2025, 1, 11), 16m);
        var sut = new PlanningService(db);

        await sut.CalculateScheduleAsync(task);
        var result = await db.Tasks.FindAsync(task.Id);

        // Sa (8h) + So (8h) → Ende Sonntag 12.01
        Assert.Equal(new DateTime(2025, 1, 12), result!.EndDate);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Gruppe 4: Sonderfälle
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Milestone_DateUnchanged_AfterCalculate()
    {
        var db = TestDb.Create(nameof(Milestone_DateUnchanged_AfterCalculate));
        await TestDb.SeedScheduleAsync(db);
        var project = await TestDb.SeedProjectAsync(db);
        var milestone = await TestDb.SeedTaskAsync(
            db, project.Id, new DateTime(2025, 3, 31), plannedDuration: null, isMilestone: true);
        var sut = new PlanningService(db);

        await sut.CalculateScheduleAsync(milestone);
        var result = await db.Tasks.FindAsync(milestone.Id);

        // Meilenstein darf nie verändert werden
        Assert.Equal(new DateTime(2025, 3, 31), result!.StartDate);
        Assert.Equal(new DateTime(2025, 3, 31), result!.EndDate);
    }

    [Fact]
    public async Task Task_WithNullDuration_DateUnchanged()
    {
        var db = TestDb.Create(nameof(Task_WithNullDuration_DateUnchanged));
        await TestDb.SeedScheduleAsync(db);
        var project = await TestDb.SeedProjectAsync(db);
        var task = await TestDb.SeedTaskAsync(
            db, project.Id, new DateTime(2025, 1, 6), plannedDuration: null);
        var sut = new PlanningService(db);

        await sut.CalculateScheduleAsync(task);
        var result = await db.Tasks.FindAsync(task.Id);

        // Keine Dauer → keine Berechnung → Datum unverändert
        Assert.Equal(new DateTime(2025, 1, 6), result!.StartDate);
    }

    [Fact]
    public async Task Task_WithZeroDuration_DateUnchanged()
    {
        var db = TestDb.Create(nameof(Task_WithZeroDuration_DateUnchanged));
        await TestDb.SeedScheduleAsync(db);
        var project = await TestDb.SeedProjectAsync(db);
        var task = await TestDb.SeedTaskAsync(db, project.Id, new DateTime(2025, 1, 6), 0m);
        var sut = new PlanningService(db);

        await sut.CalculateScheduleAsync(task);
        var result = await db.Tasks.FindAsync(task.Id);

        Assert.Equal(new DateTime(2025, 1, 6), result!.StartDate);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Gruppe 5: Vorgänger-Kaskadierung
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Successor_StartsAfterPredecessorEnd()
    {
        // Aufgabe A: Mo 06.01, 8h → endet Mo 06.01
        // Aufgabe B: Start ursprünglich Mo 06.01 (= gleicher Tag wie A)
        //            Nach Kaskade: B muss nach A-Ende starten → Di 07.01
        var db = TestDb.Create(nameof(Successor_StartsAfterPredecessorEnd));
        await TestDb.SeedScheduleAsync(db, workDaysMask: 62, dailyHours: 8m);
        var project = await TestDb.SeedProjectAsync(db);

        var taskA = await TestDb.SeedTaskAsync(db, project.Id, new DateTime(2025, 1, 6), 8m);
        var taskB = await TestDb.SeedTaskAsync(db, project.Id, new DateTime(2025, 1, 6), 16m);

        // Finish-to-Start Link: A → B
        db.TaskLinks.Add(new TaskLink
        {
            Source    = taskA.Id,
            Target    = taskB.Id,
            Type      = "0",
            ProjectId = project.Id
        });
        await db.SaveChangesAsync();

        var sut = new PlanningService(db);

        // A berechnen → B wird kaskadiert
        await sut.CalculateScheduleAsync(taskA);

        var resultA = await db.Tasks.FindAsync(taskA.Id);
        var resultB = await db.Tasks.FindAsync(taskB.Id);

// A endet Montag (06.01.)
        Assert.Equal(new DateTime(2025, 1, 6), resultA!.EndDate);

        // Korrektur: B startet am gleichen Tag, an dem A endet (Montag)
        Assert.Equal(new DateTime(2025, 1, 6), resultB!.StartDate);        
        
        // B hat 16h = 2 Arbeitstage (Mo + Di)
        // Wenn Start = Montag, dann Ende = Dienstag
        Assert.Equal(new DateTime(2025, 1, 7), resultB!.EndDate);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Gruppe 6: Schedule-Priorisierung (projektspezifisch vs. global)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProjectSpecificSchedule_HasPriorityOverGlobal()
    {
        var db = TestDb.Create(nameof(ProjectSpecificSchedule_HasPriorityOverGlobal));
        var project = await TestDb.SeedProjectAsync(db);

        // Globaler Default: 8h/Tag
        await TestDb.SeedScheduleAsync(db, workDaysMask: 62, dailyHours: 8m,
            isDefault: true, projectId: null);

        // Projektspezifischer Plan: 4h/Tag
        await TestDb.SeedScheduleAsync(db, workDaysMask: 62, dailyHours: 4m,
            isDefault: true, projectId: project.Id);

        // 8h Task auf Montag
        var task = await TestDb.SeedTaskAsync(db, project.Id, new DateTime(2025, 1, 6), 8m);
        var sut = new PlanningService(db);

        await sut.CalculateScheduleAsync(task);
        var result = await db.Tasks.FindAsync(task.Id);

        // 8h / 4h = 2 Tage → Ende Dienstag (projektspezifischer Plan wird verwendet)
        Assert.Equal(new DateTime(2025, 1, 7), result!.EndDate);
    }

    [Fact]
    public async Task GlobalDefaultSchedule_UsedWhenNoProjectSchedule()
    {
        var db = TestDb.Create(nameof(GlobalDefaultSchedule_UsedWhenNoProjectSchedule));
        var project = await TestDb.SeedProjectAsync(db);

        // Nur globaler Default: 6h/Tag
        await TestDb.SeedScheduleAsync(db, workDaysMask: 62, dailyHours: 6m,
            isDefault: true, projectId: null);

        var task = await TestDb.SeedTaskAsync(db, project.Id, new DateTime(2025, 1, 6), 12m);
        var sut = new PlanningService(db);

        await sut.CalculateScheduleAsync(task);
        var result = await db.Tasks.FindAsync(task.Id);

        // 12h / 6h = 2 Tage → Mo + Di → Ende Dienstag
        Assert.Equal(new DateTime(2025, 1, 7), result!.EndDate);
    }

    [Fact]
    public async Task FallbackSchedule_UsedWhenNoScheduleExists()
    {
        // Kein WorkSchedule in der DB → PlanningService nutzt Fallback Mo–Fr, 8h
        var db = TestDb.Create(nameof(FallbackSchedule_UsedWhenNoScheduleExists));
        var project = await TestDb.SeedProjectAsync(db);
        var task = await TestDb.SeedTaskAsync(db, project.Id, new DateTime(2025, 1, 6), 16m);
        var sut = new PlanningService(db);

        await sut.CalculateScheduleAsync(task);
        var result = await db.Tasks.FindAsync(task.Id);

        // Fallback 8h/Tag → 16h = 2 Tage → Mo + Di
        Assert.Equal(new DateTime(2025, 1, 7), result!.EndDate);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Gruppe 7: Bitmask-Korrektheit (Tagesprüfung)
    // ────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(2,  true,  "2025-01-06")] // Mo, Bit 2 → Arbeitstag
    [InlineData(62, true,  "2025-01-06")] // Mo–Fr, Bit 2 → Arbeitstag
    [InlineData(62, false, "2025-01-11")] // Mo–Fr, Sa (Bit 64) → kein Arbeitstag
    [InlineData(62, false, "2025-01-12")] // Mo–Fr, So (Bit 1)  → kein Arbeitstag
    [InlineData(65, true,  "2025-01-11")] // Sa+So (65), Sa     → Arbeitstag
    [InlineData(1,  true,  "2025-01-12")] // nur So (Bit 1), So → Arbeitstag
    public async Task BitMask_WorkDay_Detection_IsCorrect(
        int mask, bool expectedIsWorkDay, string dateStr)
    {
        var date = DateTime.Parse(dateStr);
        var db = TestDb.Create($"bitmask_{mask}_{dateStr}");
        await TestDb.SeedScheduleAsync(db, workDaysMask: mask, dailyHours: 8m);
        var project = await TestDb.SeedProjectAsync(db);

        // Wenn expectedIsWorkDay: 8h Task auf diesem Tag → EndDate = gleicher Tag
        // Wenn kein Arbeitstag: Start wird auf nächsten Arbeitstag verschoben
        var task = await TestDb.SeedTaskAsync(db, project.Id, date, 8m);
        var sut = new PlanningService(db);

        await sut.CalculateScheduleAsync(task);
        var result = await db.Tasks.FindAsync(task.Id);

        if (expectedIsWorkDay)
            Assert.Equal(date, result!.StartDate); // Datum bleibt, kein Shift
        else
            Assert.NotEqual(date, result!.StartDate); // Datum wurde auf nächsten Arbeitstag verschoben
    }
}
