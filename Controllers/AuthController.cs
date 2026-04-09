using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPlanner.Data;
using ProjectPlanner.Models;
using ProjectPlanner.Services;
using System.Text.RegularExpressions;

namespace ProjectPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtService _jwt;
    private readonly AppDbContext _db;

    public AuthController(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        JwtService jwt,
        AppDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwt = jwt;
        _db = db;
    }

    // ── Login ──────────────────────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        // ✅ FIX: Validate email and password are not empty
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { error = "E-Mail und Passwort sind erforderlich" });

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized(new { error = "Ungültige Anmeldedaten" });

        return Ok(new
        {
            token = _jwt.GenerateToken(user),
            mustChangePassword = user.MustChangePassword,
            privacyAccepted = user.PrivacyAccepted
        });
    }

    // ── Passwort ändern ──────────────────────────────────────────────────────
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        // ✅ FIX: Null check for userId
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "Benutzer-ID nicht gefunden" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { error = "Benutzer nicht gefunden" });

        // ✅ FIX: Validate password inputs
        if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
            return BadRequest(new { error = "Passwörter dürfen nicht leer sein" });

        if (dto.NewPassword.Length < 6)
            return BadRequest(new { error = "Neues Passwort muss mindestens 6 Zeichen lang sein" });

        if (!Regex.IsMatch(dto.NewPassword, @"[A-Z]"))
            return BadRequest(new { error = "Passwort muss mindestens einen Großbuchstaben enthalten" });

        if (!Regex.IsMatch(dto.NewPassword, @"[a-z]"))
            return BadRequest(new { error = "Passwort muss mindestens einen Kleinbuchstaben enthalten" });

        if (!Regex.IsMatch(dto.NewPassword, @"[0-9]"))
            return BadRequest(new { error = "Passwort muss mindestens eine Zahl enthalten" });

        if (!Regex.IsMatch(dto.NewPassword, @"[!@#$%^&*()_+\-=\[\]{};':"",.<>?/\\|`~]"))
            return BadRequest(new { error = "Passwort muss mindestens ein Sonderzeichen enthalten" });

        if (dto.CurrentPassword == dto.NewPassword)
            return BadRequest(new { error = "Neues Passwort darf nicht mit aktuellem Passwort identisch sein" });

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { error = errors });
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            message = "Passwort erfolgreich geändert.",
            token = _jwt.GenerateToken(user)
        });
    }

    // ── Datenschutz akzeptieren ──────────────────────────────────────────────
    [HttpPost("accept-privacy")]
    [Authorize]
    public async Task<IActionResult> AcceptPrivacy()
    {
        // ✅ FIX: Null check for userId
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "Benutzer-ID nicht gefunden" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { error = "Benutzer nicht gefunden" });

        user.PrivacyAccepted = true;
        await _userManager.UpdateAsync(user);

        _db.PrivacyConsents.Add(new PrivacyConsent
        {
            UserId = user.Id,
            AcceptedAt = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            Version = "1.0",
            Accepted = true
        });
        await _db.SaveChangesAsync();

        return Ok(new { message = "Datenschutz akzeptiert." });
    }

    // ── Eigenes Profil abrufen ───────────────────────────────────────────────
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        // ✅ FIX: Null check for userId
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "Benutzer-ID nicht gefunden" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { error = "Benutzer nicht gefunden" });

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
            .Select(u => new { u.Id, u.FullName, u.ShortName, u.HourlyRate, u.Email, u.Role })
            .ToListAsync();
        return Ok(users);
    }

    // ── Rolle eines Benutzers ändern (nur Admin) ─────────────────────────────
    [HttpPut("setRole")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetRole(SetRoleDto dto)
    {
        // ✅ FIX: Validate role
        if (string.IsNullOrWhiteSpace(dto.Role) || !new[] { "Admin", "Teacher", "Student" }.Contains(dto.Role))
            return BadRequest(new { error = "Ungültige Rolle. Erlaubt: Admin, Teacher, Student" });

        // ✅ FIX: Validate UserId
        if (string.IsNullOrWhiteSpace(dto.UserId))
            return BadRequest(new { error = "Benutzer-ID erforderlich" });

        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null)
            return NotFound(new { error = "Benutzer nicht gefunden" });

        // ✅ FIX: Prevent removing last admin
        if (dto.Role != "Admin")
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var adminCount = admins.Count;

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains("Admin") && adminCount <= 1)
                return BadRequest(new { error = "Der letzte Administrator kann nicht entfernt werden!" });
        }

        var rolesBefore = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, rolesBefore);

        await EnsureRoleExists(dto.Role);
        await _userManager.AddToRoleAsync(user, dto.Role);

        user.Role = dto.Role;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = $"Rolle auf '{dto.Role}' gesetzt." });
    }

    // ── Benutzer durch Admin anlegen (mit Standardpasswort) ─────────────────
    [HttpPost("create-user")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {
        // ✅ FIX: Validate all inputs
        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { error = "E-Mail-Adresse ist erforderlich" });

        if (!dto.Email.Contains("@") || !dto.Email.Contains("."))
            return BadRequest(new { error = "Ungültige E-Mail-Adresse. Beispiel: max@schule.at" });

        if (string.IsNullOrWhiteSpace(dto.FullName) || dto.FullName.Length < 3)
            return BadRequest(new { error = "Name muss mindestens 3 Zeichen lang sein" });

        if (dto.FullName.Length > 100)
            return BadRequest(new { error = "Name darf maximal 100 Zeichen lang sein" });

        if (string.IsNullOrWhiteSpace(dto.Role) || !new[] { "Admin", "Teacher", "Student" }.Contains(dto.Role))
            return BadRequest(new { error = "Ungültige Rolle. Erlaubt: Admin, Teacher, Student" });

        if (dto.HourlyRate.HasValue && (dto.HourlyRate < 0 || dto.HourlyRate > 999.99m))
            return BadRequest(new { error = "Stundensatz muss zwischen 0 und 999,99€ liegen" });

        // ✅ FIX: Check if email already exists
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return BadRequest(new { error = "Diese E-Mail-Adresse wird bereits verwendet" });

        var user = new AppUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            ShortName = dto.ShortName ?? "",
            HourlyRate = dto.HourlyRate ?? 0,
            Role = dto.Role,
            MustChangePassword = true,
            PrivacyAccepted = false
        };

        var defaultPassword = "!Schule" + DateTime.Now.Year + "!";
        var result = await _userManager.CreateAsync(user, defaultPassword);

        // ✅ FIX: Better error handling
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new
            {
                error = "Benutzer konnte nicht erstellt werden",
                details = errors
            });
        }

        await EnsureRoleExists(dto.Role);
        await _userManager.AddToRoleAsync(user, dto.Role);

        return Ok(new
        {
            message = $"Benutzer '{dto.FullName}' erfolgreich angelegt.",
            userId = user.Id,
            defaultPassword,
            mustChangePassword = true
        });
    }

    // ── Passwort eines Benutzers zurücksetzen (nur Admin) ──────────────────
    [HttpPost("reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserId))
            return BadRequest(new { error = "Benutzer-ID erforderlich" });

        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null)
            return NotFound(new { error = "Benutzer nicht gefunden" });

        var defaultPassword = "!Schule" + DateTime.Now.Year + "!";

        // Passwort über Token-Mechanismus zurücksetzen
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, defaultPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { error = "Passwort konnte nicht zurückgesetzt werden", details = errors });
        }

        user.MustChangePassword = true;
        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            message = $"Passwort von '{user.FullName}' wurde zurückgesetzt.",
            defaultPassword
        });
    }

    private async Task EnsureRoleExists(string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole(role));
    }
}

public record LoginDto(string Email, string Password);
public record SetRoleDto(string UserId, string Role);
public record ChangePasswordDto(string CurrentPassword, string NewPassword);
public record CreateUserDto(string Email, string FullName, string Role, string? ShortName = null, decimal? HourlyRate = null);
public record ResetPasswordDto(string UserId);