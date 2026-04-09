using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectPlanner.Data;
using ProjectPlanner.Middleware;
using ProjectPlanner.Models;
using ProjectPlanner.Services;
using System.Text;

// 🔥 .env laden
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────
// 🔐 ENV / CONFIG
// ─────────────────────────────────────────────

var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["Jwt:Key"];

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("Default");

var allowedOriginsEnv = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS") ?? "";

if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("JWT key not configured!");

if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("Database connection string not configured!");

// ─────────────────────────────────────────────
// 🛢 DATABASE
// ─────────────────────────────────────────────

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseOracle(connectionString,
        b => b.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)));

// ─────────────────────────────────────────────
// 👤 IDENTITY
// ─────────────────────────────────────────────

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit           = true;
    options.Password.RequiredLength         = 12;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase       = true;
    options.Password.RequireLowercase       = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ─────────────────────────────────────────────
// 🔐 JWT AUTH
// ─────────────────────────────────────────────

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = builder.Configuration["Jwt:Issuer"],
        ValidAudience            = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// ─────────────────────────────────────────────
// 🔒 AUTHORIZATION
// ─────────────────────────────────────────────

builder.Services.AddAuthorization();

// ─────────────────────────────────────────────
// 🧠 SERVICES
// ─────────────────────────────────────────────

builder.Services.AddScoped<JwtService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ─────────────────────────────────────────────
// 🌍 CORS
// ─────────────────────────────────────────────

var origins = allowedOriginsEnv
    .Split(",", StringSplitOptions.RemoveEmptyEntries)
    .Select(o => o.Trim())
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        if (origins.Length > 0)
            policy.WithOrigins(origins);
        else
            policy.AllowAnyOrigin(); // fallback (nur dev!)

        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ─────────────────────────────────────────────
// 🛡 RATE LIMITING
// ─────────────────────────────────────────────

var rateLimitStore = new Dictionary<string, List<DateTime>>();
builder.Services.AddSingleton(rateLimitStore);

var app = builder.Build();

// ─────────────────────────────────────────────
// 🚦 MIDDLEWARE
// ─────────────────────────────────────────────

app.UseRateLimiting();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "ProjectPlanner API v1");
        c.RoutePrefix = "swagger";
    });
}

// 🌍 CORS
app.UseCors("CorsPolicy");

// 📁 Static files
app.UseDefaultFiles();
app.UseStaticFiles();

// 🔐 Auth
app.UseAuthentication();
app.UseAuthorization();

// ─────────────────────────────────────────────
// 👑 ROLES & SEEDING
// ─────────────────────────────────────────────

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var role in new[] { "Admin", "Teacher", "Student" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!await db.WorkSchedules.AnyAsync())
    {
        db.WorkSchedules.Add(new WorkSchedule
        {
            Name           = "Standard-Woche (Mo–Fr)",
            ProjectId      = null,
            WorkDaysMask   = 62,
            DailyStartTime = "08:00",
            DailyEndTime   = "17:00",
            DailyHours     = 8,
            IsDefault      = true
        });

        await db.SaveChangesAsync();
    }
}

// ─────────────────────────────────────────────
// 🧭 ROUTING
// ─────────────────────────────────────────────

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();