using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectPlanner.Data;
using ProjectPlanner.Models;
using ProjectPlanner.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Oracle Datenbank ──────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseOracle(builder.Configuration.GetConnectionString("Default"),
        b => b.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)));

// ── ASP.NET Identity (mit RoleManager) ───────────────────────────────────────
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit           = true;
    options.Password.RequiredLength         = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase       = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;

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
    // Token auch aus Query-String lesen (für Datei-Downloads)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            if (ctx.Request.Query.TryGetValue("token", out var token))
                ctx.Token = token;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── OpenAPI / Swagger ─────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen();

// ── CORS (für lokale Entwicklung) ─────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevPolicy", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

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
            Name           = "Standard-Woche (Mo–Fr)",
            ProjectId      = null,
            WorkDaysMask   = 62, // Mo=2, Di=4, Mi=8, Do=16, Fr=32
            DailyStartTime = "08:00",
            DailyEndTime   = "17:00",
            DailyHours     = 8,
            IsDefault      = true
        });
        await db.SaveChangesAsync();
    }
}

// ── Middleware Pipeline ───────────────────────────────────────────────────────
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

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();
