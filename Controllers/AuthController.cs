using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPlanner.Data;
using ProjectPlanner.Models;
using ProjectPlanner.Services;

namespace ProjectPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser>      _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtService                _jwt;
    private readonly AppDbContext              _db;

    public AuthController(
        UserManager<AppUser>      userManager,
        RoleManager<IdentityRole> roleManager,
        JwtService                jwt,
        AppDbContext              db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwt         = jwt;
        _db          = db;
    }

    // ── Registrieren ──────────────────────────────────────────────────────────
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (!dto.PrivacyAccepted)
            return BadRequest(new { error = "Datenschutzbestimmungen müssen akzeptiert werden." });

        var users   = await _userManager.Users.ToListAsync();
        var isFirst = users.Count == 0;

        var user = new AppUser
        {
            UserName           = dto.Email,
            Email              = dto.Email,
            FullName           = dto.FullName,
            Role               = isFirst ? "Admin" : "Student",
            MustChangePassword = !isFirst,
            PrivacyAccepted    = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var roleName = isFirst ? "Admin" : "Student";
        await EnsureRoleExists(roleName);
        await _userManager.AddToRoleAsync(user, roleName);

        // Datenschutz-Zustimmung protokollieren
        _db.PrivacyConsents.Add(new PrivacyConsent
        {
            UserId     = user.Id,
            AcceptedAt = DateTime.UtcNow,
            IpAddress  = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Version    = "1.0",
            Accepted   = true
        });
        await _db.SaveChangesAsync();

        return Ok(new
        {
            token              = _jwt.GenerateToken(user),
            mustChangePassword = user.MustChangePassword
        });
    }

    // ── Login ────────────────────────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized(new { error = "Ungültige Anmeldedaten" });

        return Ok(new
        {
            token              = _jwt.GenerateToken(user),
            mustChangePassword = user.MustChangePassword,
            privacyAccepted    = user.PrivacyAccepted
        });
    }

    // ── Passwort ändern ──────────────────────────────────────────────────────
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user   = await _userManager.FindByIdAsync(userId!);
        if (user == null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded) return BadRequest(result.Errors);

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = "Passwort erfolgreich geändert.", token = _jwt.GenerateToken(user) });
    }

    // ── Datenschutz akzeptieren ──────────────────────────────────────────────
    [HttpPost("accept-privacy")]
    [Authorize]
    public async Task<IActionResult> AcceptPrivacy()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user   = await _userManager.FindByIdAsync(userId!);
        if (user == null) return NotFound();

        user.PrivacyAccepted = true;
        await _userManager.UpdateAsync(user);

        _db.PrivacyConsents.Add(new PrivacyConsent
        {
            UserId     = user.Id,
            AcceptedAt = DateTime.UtcNow,
            IpAddress  = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Version    = "1.0",
            Accepted   = true
        });
        await _db.SaveChangesAsync();

        return Ok(new { message = "Datenschutz akzeptiert." });
    }

    // ── Eigenes Profil abrufen ───────────────────────────────────────────────
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user   = await _userManager.FindByIdAsync(userId!);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.MustChangePassword,
            user.PrivacyAccepted
        });
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

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        await EnsureRoleExists(dto.Role);
        await _userManager.AddToRoleAsync(user, dto.Role);

        user.Role = dto.Role;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = $"Rolle auf '{dto.Role}' gesetzt.", token = _jwt.GenerateToken(user) });
    }

    // ── Benutzer durch Admin anlegen (mit Standardpasswort) ─────────────────
    [HttpPost("create-user")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {
        if (!new[] { "Admin", "Teacher", "Student" }.Contains(dto.Role))
            return BadRequest(new { error = "Ungültige Rolle." });

        var user = new AppUser
        {
            UserName           = dto.Email,
            Email              = dto.Email,
            FullName           = dto.FullName,
            Role               = dto.Role,
            MustChangePassword = true,
            PrivacyAccepted    = false
        };

        const string defaultPassword = "Schule2024!";
        var result = await _userManager.CreateAsync(user, defaultPassword);
        if (!result.Succeeded) return BadRequest(result.Errors);

        await EnsureRoleExists(dto.Role);
        await _userManager.AddToRoleAsync(user, dto.Role);

        return Ok(new
        {
            message            = $"Benutzer '{dto.FullName}' angelegt.",
            userId             = user.Id,
            defaultPassword,
            mustChangePassword = true
        });
    }

    private async Task EnsureRoleExists(string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole(role));
    }
}

public record RegisterDto(string Email, string Password, string FullName, bool PrivacyAccepted);
public record LoginDto(string Email, string Password);
public record SetRoleDto(string UserId, string Role);
public record ChangePasswordDto(string CurrentPassword, string NewPassword);
public record CreateUserDto(string Email, string FullName, string Role);
