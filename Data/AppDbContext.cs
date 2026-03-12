using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectPlanner.Models;

namespace ProjectPlanner.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Project>       Projects       => Set<Project>();
    public DbSet<ProjectTask>   Tasks          => Set<ProjectTask>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskLink>      TaskLinks      => Set<TaskLink>();
    public DbSet<TaskComment>   TaskComments   => Set<TaskComment>();

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

        // ── Beziehungen ──────────────────────────────────────────────────────
        builder.Entity<Project>()
            .HasOne(p => p.Owner).WithMany(u => u.Projects)
            .HasForeignKey(p => p.OwnerId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectTask>()
            .HasOne(t => t.Project).WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectTask>()
            .HasOne(t => t.Assignee).WithMany()
            .HasForeignKey(t => t.AssigneeId).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ProjectMember>()
            .HasKey(m => new { m.ProjectId, m.UserId });
        builder.Entity<ProjectMember>()
            .HasOne(m => m.Project).WithMany(p => p.Members)
            .HasForeignKey(m => m.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ProjectMember>()
            .HasOne(m => m.User).WithMany()
            .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);

        // TaskLink – Source/Target ohne Cascade (sonst Oracle-Konflikt)
        builder.Entity<TaskLink>()
            .HasOne(l => l.SourceTask).WithMany(t => t.LinksFrom)
            .HasForeignKey(l => l.Source).OnDelete(DeleteBehavior.NoAction);
        builder.Entity<TaskLink>()
            .HasOne(l => l.TargetTask).WithMany(t => t.LinksTo)
            .HasForeignKey(l => l.Target).OnDelete(DeleteBehavior.NoAction);
        builder.Entity<TaskLink>()
            .HasOne(l => l.Project).WithMany(p => p.Links)
            .HasForeignKey(l => l.ProjectId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TaskComment>()
            .HasOne(c => c.Task).WithMany(t => t.Comments)
            .HasForeignKey(c => c.TaskId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<TaskComment>()
            .HasOne(c => c.User).WithMany()
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);

        // ── Feldlängen ───────────────────────────────────────────────────────
        builder.Entity<AppUser>().Property(u => u.FullName).HasMaxLength(100);
        builder.Entity<AppUser>().Property(u => u.Role).HasMaxLength(20);
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
