using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ProjectPlanner.Models;

namespace ProjectPlanner.Services;

public class JwtService
{
    private readonly IConfiguration      _config;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IConfiguration config, UserManager<AppUser> userManager, ILogger<JwtService> logger)
    {
        _config      = config;
        _userManager = userManager;
        _logger      = logger;
    }

    /// <summary>
    /// Generiert ein JWT mit den Identity-Rollen aus der Datenbank.
    /// SECURITY FIXES:
    /// - Reads JWT secret from JWT_SECRET_KEY environment variable
    /// - Token expiry reduced from 8h to 4h
    /// - Added logging and error handling
    /// </summary>
    public string GenerateToken(AppUser user)
    {
        var roles = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();
        var role  = roles.FirstOrDefault() ?? user.Role ?? "Student";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email,          user.Email ?? string.Empty),
            new(ClaimTypes.Name,           user.FullName),
            new(ClaimTypes.Role,           role),
            new("mustChangePassword",      user.MustChangePassword.ToString().ToLower()),
            new("privacyAccepted",         user.PrivacyAccepted.ToString().ToLower())
        };

        // SECURITY FIX: Read from environment variable first
        var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
            ?? _config["Jwt:Key"];
        
        if (string.IsNullOrEmpty(jwtKey))
        {
            _logger.LogError("SECURITY: JWT_SECRET_KEY not configured");
            throw new InvalidOperationException("JWT secret key is not configured");
        }

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var cred  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // SECURITY FIX: Reduced expiry from 8 hours to 4 hours
        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(4),
            signingCredentials: cred);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}