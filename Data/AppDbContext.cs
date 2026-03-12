using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectPlanner.Models;

namespace ProjectPlanner.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Project>        Projects        => Set<Project>();
    public DbSet<ProjectTask>    Tasks           => Set<ProjectTask>();
    public DbSet<ProjectMember>  ProjectMembers  => Set<ProjectMember>();
    public DbSet<TaskLink>       TaskLinks       => Set<TaskLink>();
    public DbSet<TaskComment>    TaskComments    => Set<TaskComment>();
    public DbSet<PrivacyConsent> PrivacyConsents => Set<PrivacyConsent>();
    public DbSet<WorkSchedule>   WorkSchedules   => Set<WorkSchedule>();
    public DbSet<TimeEntry>      TimeEntries     => Set<TimeEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("ADMIN");
        base.OnModelCreating(builder);

        // ── Oracle Tabellennamen ─────────────────────────────────────────────
        builder.Entity<AppUser>().ToTable("ASPNETUSERS");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().ToTable("ASPNETROLES");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("ASPNETUSERROLES");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("ASPNETUSERCLAIMS");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("ASPNETUSERLOGINS");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("ASPNETUSERTOKENS");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("ASPNETROLECLAIMS");
        builder.Entity<Project>().ToTable("PROJECTS");
        builder.Entity<ProjectTask>().ToTable("TASKS");
        builder.Entity<ProjectMember>().ToTable("PROJECT_MEMBERS");
        builder.Entity<TaskLink>().ToTable("TASK_LINKS");
        builder.Entity<TaskComment>().ToTable("TASK_COMMENTS");
        builder.Entity<PrivacyConsent>().ToTable("PRIVACY_CONSENTS");
        builder.Entity<WorkSchedule>().ToTable("WORK_SCHEDULES");
        builder.Entity<TimeEntry>().ToTable("TIME_ENTRIES");

        // ── AppUser: neue Felder ─────────────────────────────────────────────
        builder.Entity<AppUser>().Property(u => u.FullName).HasMaxLength(100);
        builder.Entity<AppUser>().Property(u => u.Role).HasMaxLength(20);

        // ── Projekt-Beziehungen ──────────────────────────────────────────────
        builder.Entity<Project>()
            .HasOne(p => p.Owner).WithMany(u => u.Projects)
            .HasForeignKey(p => p.OwnerId).OnDelete(DeleteBehavior.Cascade);

        // ── Task-Beziehungen ─────────────────────────────────────────────────
        builder.Entity<ProjectTask>()
            .HasOne(t => t.Project).WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectTask>()
            .HasOne(t => t.Assignee).WithMany()
            .HasForeignKey(t => t.AssigneeId).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ProjectTask>().Property(t => t.PlannedDuration).HasColumnType("NUMBER(10,2)");
        builder.Entity<ProjectTask>().Property(t => t.ActualDuration).HasColumnType("NUMBER(10,2)");
        builder.Entity<ProjectTask>().Property(t => t.WorkSharePercent).HasColumnType("NUMBER(5,2)");

        // ── ProjectMember ────────────────────────────────────────────────────
        builder.Entity<ProjectMember>()
            .HasKey(m => new { m.ProjectId, m.UserId });
        builder.Entity<ProjectMember>()
            .HasOne(m => m.Project).WithMany(p => p.Members)
            .HasForeignKey(m => m.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ProjectMember>()
            .HasOne(m => m.User).WithMany()
            .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ProjectMember>().Property(m => m.JoinedAt);

        // ── TaskLink – No Cascade wegen Oracle Self-Join-Konflikt ────────────
        builder.Entity<TaskLink>()
            .HasOne(l => l.SourceTask).WithMany(t => t.LinksFrom)
            .HasForeignKey(l => l.Source).OnDelete(DeleteBehavior.NoAction);
        builder.Entity<TaskLink>()
            .HasOne(l => l.TargetTask).WithMany(t => t.LinksTo)
            .HasForeignKey(l => l.Target).OnDelete(DeleteBehavior.NoAction);
        builder.Entity<TaskLink>()
            .HasOne(l => l.Project).WithMany(p => p.Links)
            .HasForeignKey(l => l.ProjectId).OnDelete(DeleteBehavior.Cascade);

        // ── TaskComment ──────────────────────────────────────────────────────
        builder.Entity<TaskComment>()
            .HasOne(c => c.Task).WithMany(t => t.Comments)
            .HasForeignKey(c => c.TaskId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<TaskComment>()
            .HasOne(c => c.User).WithMany()
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);

        // ── PrivacyConsent ───────────────────────────────────────────────────
        builder.Entity<PrivacyConsent>()
            .HasOne(pc => pc.User).WithMany(u => u.PrivacyConsents)
            .HasForeignKey(pc => pc.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<PrivacyConsent>().Property(pc => pc.Version).HasMaxLength(20);
        builder.Entity<PrivacyConsent>().Property(pc => pc.IpAddress).HasMaxLength(45);

        // ── WorkSchedule ─────────────────────────────────────────────────────
        builder.Entity<WorkSchedule>()
            .HasOne(ws => ws.Project).WithMany(p => p.WorkSchedules)
            .HasForeignKey(ws => ws.ProjectId).OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
        builder.Entity<WorkSchedule>().Property(ws => ws.Name).HasMaxLength(100);
        builder.Entity<WorkSchedule>().Property(ws => ws.DailyStartTime).HasMaxLength(5);
        builder.Entity<WorkSchedule>().Property(ws => ws.DailyEndTime).HasMaxLength(5);
        builder.Entity<WorkSchedule>().Property(ws => ws.DailyHours).HasColumnType("NUMBER(4,2)");

        // ── TimeEntry ────────────────────────────────────────────────────────
        builder.Entity<TimeEntry>()
            .HasOne(te => te.User).WithMany(u => u.TimeEntries)
            .HasForeignKey(te => te.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<TimeEntry>()
            .HasOne(te => te.Task).WithMany(t => t.TimeEntries)
            .HasForeignKey(te => te.TaskId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<TimeEntry>()
            .HasOne(te => te.Project).WithMany(p => p.TimeEntries)
            .HasForeignKey(te => te.ProjectId).OnDelete(DeleteBehavior.NoAction);
        builder.Entity<TimeEntry>().Property(te => te.DurationHours).HasColumnType("NUMBER(8,2)");
        builder.Entity<TimeEntry>().Property(te => te.Description).HasMaxLength(500);

        // ── Feldlängen (bestehend) ───────────────────────────────────────────
        builder.Entity<Project>().Property(p => p.Name).HasMaxLength(200);
        builder.Entity<Project>().Property(p => p.Description).HasMaxLength(4000);
        builder.Entity<Project>().Property(p => p.Color).HasMaxLength(20);
        builder.Entity<ProjectTask>().Property(t => t.Title).HasMaxLength(200);
        builder.Entity<ProjectTask>().Property(t => t.Priority).HasMaxLength(10);
        builder.Entity<ProjectTask>().Property(t => t.Status).HasMaxLength(20);
        builder.Entity<ProjectTask>().Property(t => t.Note).HasMaxLength(2000);
        builder.Entity<TaskLink>().Property(l => l.Type).HasMaxLength(5);
        builder.Entity<TaskComment>().Property(c => c.Text).HasMaxLength(2000);
    }
}
