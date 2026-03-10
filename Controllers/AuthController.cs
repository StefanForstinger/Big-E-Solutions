using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPlanner.Models;
using ProjectPlanner.Services;

namespace ProjectPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser>  _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtService            _jwt;

    public AuthController(
        UserManager<AppUser>      userManager,
        RoleManager<IdentityRole> roleManager,
        JwtService                jwt)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwt         = jwt;
    }

    // ── Registrieren ────────────────────────────────────────────────────────
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var user = new AppUser
        {
            UserName = dto.Email,
            Email    = dto.Email,
            FullName = dto.FullName,
            Role     = "Student"           // Standardrolle
        };

        var users = await _userManager.Users.ToListAsync();
        if(users.Count == 0)
        {
            user.Role = "Admin"; // Erster registrierter Benutzer wird Admin
        }

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        if (users.Count == 0)
        {
            await EnsureRoleExists("Admin");
            await _userManager.AddToRoleAsync(user, "Admin");
        }
        else
        {
            await EnsureRoleExists("Student");
            await _userManager.AddToRoleAsync(user, "Student");
        }

        return Ok(new { token = _jwt.GenerateToken(user) });
    }

    // ── Login ────────────────────────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized(new { error = "Ungültige Anmeldedaten" });

        return Ok(new { token = _jwt.GenerateToken(user) });
    }

    // ── Alle Benutzer abrufen (nur Admin/Teacher) ────────────────────────────
    [HttpGet("getAll")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userManager.Users
            .Select(u => new { u.Id, u.FullName, u.Email, u.Role })
            .ToListAsync();
        return Ok(users);
    }

    // ── Rolle eines Benutzers ändern (nur Admin) ─────────────────────────────
    [HttpPut("setRole")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetRole(SetRoleDto dto)
    {
        if (!new[] { "Admin", "Teacher", "Student" }.Contains(dto.Role))
            return BadRequest(new { error = "Ungültige Rolle. Erlaubt: Admin, Teacher, Student" });

        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null) return NotFound(new { error = "Benutzer nicht gefunden" });

        // Aus allen bestehenden Rollen entfernen
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // Neue Rolle vergeben
        await EnsureRoleExists(dto.Role);
        await _userManager.AddToRoleAsync(user, dto.Role);

        user.Role = dto.Role;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = $"Rolle auf '{dto.Role}' gesetzt.", token = _jwt.GenerateToken(user) });
    }

    // ── Hilfsmethode: Rolle anlegen wenn nicht vorhanden ────────────────────
    private async Task EnsureRoleExists(string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole(role));
    }
}

public record RegisterDto(string Email, string Password, string FullName);
public record LoginDto(string Email, string Password);
public record SetRoleDto(string UserId, string Role);
