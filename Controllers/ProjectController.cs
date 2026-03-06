using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPlanner.Data;
using ProjectPlanner.Models;

namespace ProjectPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectController : ControllerBase
{
    private readonly AppDbContext         _db;
    private readonly UserManager<AppUser> _userManager;

    public ProjectController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db          = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role   = User.FindFirstValue(ClaimTypes.Role)!;

        var query = _db.Projects
            .Include(p => p.Tasks)
            .Include(p => p.Owner)
            .Include(p => p.Members).ThenInclude(m => m.User)
            .OrderBy(p => p.StartDate);

        List<Project> projects = role is "Admin" or "Teacher"
            ? await query.ToListAsync()
            : await query.Where(p => p.OwnerId == userId || p.Members.Any(m => m.UserId == userId)).ToListAsync();

        var result = projects.Select(p => new
        {
            p.Id, p.Name, p.Description, p.StartDate, p.EndDate, p.OwnerId, p.Color,
            owner    = p.Owner == null ? null : new { p.Owner.FullName },
            tasks    = p.Tasks.Select(t => new { t.Id, t.Progress }),
            members  = p.Members.Select(m => new { m.UserId, m.User.FullName, m.User.Email }),
            progress = p.Tasks.Any() ? (int)p.Tasks.Average(t => t.Progress) : 0
        });

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role    = User.FindFirstValue(ClaimTypes.Role)!;
        var project = await _db.Projects.Include(p => p.Tasks).Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == id);

        if (project == null) return NotFound();
        if (project.OwnerId != userId && role is not ("Admin" or "Teacher") && !project.Members.Any(m => m.UserId == userId))
            return Forbid();

        return Ok(project);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Create([FromBody] ProjectDto dto)
    {
        var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var project = new Project
        {
            Name        = dto.Name,
            Description = dto.Description,
            StartDate   = dto.StartDate,
            EndDate     = dto.EndDate,
            Color       = dto.Color ?? "#2D9CDB",
            OwnerId     = userId
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = project.Id }, project);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProjectDto dto)
    {
        var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var project = await _db.Projects.FindAsync(id);

        if (project == null) return NotFound();
        if (project.OwnerId != userId && !User.IsInRole("Admin")) return Forbid();

        project.Name        = dto.Name;
        project.Description = dto.Description;
        project.StartDate   = dto.StartDate;
        project.EndDate     = dto.EndDate;
        if (dto.Color != null) project.Color = dto.Color;

        await _db.SaveChangesAsync();
        return Ok(project);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var project = await _db.Projects.FindAsync(id);

        if (project == null) return NotFound();
        if (project.OwnerId != userId && !User.IsInRole("Admin")) return Forbid();

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/members")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> AddMember(int id, [FromBody] MemberDto dto)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null) return NotFound(new { error = "Projekt nicht gefunden." });

        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null) return NotFound(new { error = "Benutzer nicht gefunden." });

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains("Student"))
            return BadRequest(new { error = "Nur Schüler können einem Projekt zugewiesen werden." });

        var already = await _db.ProjectMembers.AnyAsync(m => m.ProjectId == id && m.UserId == dto.UserId);
        if (already) return Conflict(new { error = $"{user.FullName} ist bereits zugewiesen." });

        _db.ProjectMembers.Add(new ProjectMember { ProjectId = id, UserId = dto.UserId });
        await _db.SaveChangesAsync();

        return Ok(new { message = $"{user.FullName} wurde hinzugefügt.", userId = user.Id, fullName = user.FullName });
    }

    [HttpDelete("{id:int}/members/{userId}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> RemoveMember(int id, string userId)
    {
        var member = await _db.ProjectMembers.FirstOrDefaultAsync(m => m.ProjectId == id && m.UserId == userId);
        if (member == null) return NotFound(new { error = "Zuweisung nicht gefunden." });

        _db.ProjectMembers.Remove(member);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:int}/members")]
    public async Task<IActionResult> GetMembers(int id)
    {
        var members = await _db.ProjectMembers
            .Where(m => m.ProjectId == id)
            .Include(m => m.User)
            .Select(m => new { m.UserId, m.User.FullName, m.User.Email })
            .ToListAsync();
        return Ok(members);
    }
}

public record ProjectDto(string Name, string Description, DateTime StartDate, DateTime EndDate, string? Color = null);
public record MemberDto(string UserId);
