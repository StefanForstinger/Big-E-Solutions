using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectPlanner.Data;
using ProjectPlanner.Middleware;
using ProjectPlanner.Models;
using ProjectPlanner.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Oracle Datenbank ────────────────────────────────────────────────────────────
// SECURITY FIX: Read connection string from environment or config
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrEmpty(connectionString) && builder.Environment.IsProduction())
{
    throw new InvalidOperationException("DB_CONNECTION_STRING environment variable must be set in production!");
}

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseOracle(connectionString,
        b => b.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)));

// ── ASP.NET Identity (mit RoleManager) ───────────────────────────────────────
// SECURITY FIX: Increased password requirements
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;                  // CHANGED: now required
    options.Password.RequireUppercase = true;                  // CHANGED: now required
    options.Password.RequireLowercase = true;                  // NEW: Added
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ── JWT Authentication ───────────────────────────────────────────────────────────
// SECURITY FIX: Use environment variable for JWT secret
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["Jwt:Key"];

// SECURITY FIX: Production safety check
if (builder.Environment.IsProduction() && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_SECRET_KEY")))
{
    throw new InvalidOperationException("CRITICAL: JWT_SECRET_KEY environment variable must be set in production!");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
    };

    // SECURITY FIX: Removed query string token reading
    // Prevents sensitive tokens from being logged in server logs and browser history
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<ProjectPlanner.Services.PlanningService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── OpenAPI / Swagger ────────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen();

// ── CORS Configuration ───────────────────────────────────────────────────────────
// SECURITY FIX: Proper CORS policy with restricted origins
builder.Services.AddCors(options =>
{
    // Development policy - for localhost only
    options.AddPolicy("DevPolicy", policy =>
        policy
            .WithOrigins("http://localhost:3000", "http://localhost:5000", "https://localhost:5001")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());

    // Production policy - use environment variable for allowed origins
    var allowedOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
        ?? "https://projectplanner-cjcvhqc0creuhcc0.westeurope-01.azurewebsites.net";

    var origins = allowedOrigins.Split(",", StringSplitOptions.RemoveEmptyEntries)
        .Select(o => o.Trim())
        .ToArray();

    options.AddPolicy("ProductionPolicy", policy =>
        policy
            .WithOrigins(origins)
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Content-Type", "Authorization")
            .AllowCredentials());
});

// ── Rate Limiting (manuell ohne externe Library) ──────────────────────────────────
// SECURITY FIX: Prevent brute force attacks
var rateLimitStore = new Dictionary<string, List<DateTime>>();
builder.Services.AddSingleton(rateLimitStore);

var app = builder.Build();

// ── Rate Limiting Middleware ────────────────────────────────────────────────────
app.UseRateLimiting();

// ── Rollen beim Start sicherstellen ──────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "Teacher", "Student" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Standard-Arbeitszeitplan anlegen wenn noch keiner existiert
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!await db.WorkSchedules.AnyAsync())
    {
        db.WorkSchedules.Add(new ProjectPlanner.Models.WorkSchedule
        {
            Name = "Standard-Woche (Mo–Fr)",
            ProjectId = null,
            WorkDaysMask = 62,
            DailyStartTime = "08:00",
            DailyEndTime = "17:00",
            DailyHours = 8,
            IsDefault = true
        });
        await db.SaveChangesAsync();
    }
}

// ── Middleware Pipeline ───────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "ProjectPlanner API v1");
        c.RoutePrefix = "swagger";
    });
    app.UseCors("DevPolicy");
}
else
{
    // SECURITY FIX: Apply production CORS policy in production
    app.UseCors("ProductionPolicy");
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();