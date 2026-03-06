using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ProjectPlanner.Models;

namespace ProjectPlanner.Services;

public class JwtService
{
    private readonly IConfiguration       _config;
    private readonly UserManager<AppUser>  _userManager;

    public JwtService(IConfiguration config, UserManager<AppUser> userManager)
    {
        _config      = config;
        _userManager = userManager;
    }

    /// <summary>Generiert ein JWT mit den Identity-Rollen aus der Datenbank.</summary>
    public string GenerateToken(AppUser user)
    {
        // Identity-Rollen synchron aus DB lesen
        var roles = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();
        var role  = roles.FirstOrDefault() ?? user.Role ?? "Student";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email,          user.Email ?? string.Empty),
            new(ClaimTypes.Name,           user.FullName),
            new(ClaimTypes.Role,           role)
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var cred  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(8),
            signingCredentials: cred);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
