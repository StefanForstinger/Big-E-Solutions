# Projektplanungssoftware – ASP.NET Core 10 + Oracle

Weborientierte Projektplanungssoftware für den Unterrichtseinsatz.
Ablösung von **ProjectLibre** durch eine stabile Eigenentwicklung der Berufsschule.

## Technologien

| Bereich | Technologie |
|---|---|
| Framework | ASP.NET Core 10 (.NET 10) |
| ORM | Entity Framework Core 10 (Oracle Provider) |
| Datenbank | Oracle XE 21c / Oracle DB 19c+ |
| Authentifizierung | ASP.NET Identity + JWT Bearer |
| Frontend | HTML5, CSS3, Vanilla JavaScript |
| Gantt-Diagramm | DHTMLX Gantt (kostenlos) |
| API-Docs | OpenAPI (built-in .NET 10) + Swagger UI |

## Projektstruktur

```
ProjectPlanner/
├── Controllers/
│   ├── AuthController.cs       # Login, Register
│   ├── ProjectController.cs    # CRUD Projekte
│   └── TaskController.cs       # CRUD Aufgaben (Gantt)
├── Models/
│   ├── AppUser.cs              # Identity User + Rolle
│   ├── Project.cs
│   └── ProjectTask.cs
├── Data/
│   └── AppDbContext.cs         # EF Core + Oracle Konfiguration
├── Services/
│   └── JwtService.cs           # JWT Token-Generierung
├── wwwroot/                    # Statisches Frontend
│   ├── index.html              # Login / Registrierung
│   ├── css/style.css
│   ├── js/api.js               # Fetch-Wrapper + Hilfsfunktionen
│   └── pages/
│       ├── dashboard.html      # Projektübersicht
│       └── gantt.html          # Gantt-Diagramm
├── Program.cs                  # App-Konfiguration
├── appsettings.json
└── ProjectPlanner.csproj
```

## Rollen

| Rolle | Rechte |
|---|---|
| Student | Eigene Projekte & Aufgaben erstellen/bearbeiten |
| Teacher | Alle Projekte lesen (kein Löschen fremder Projekte) |
| Admin | Voller Zugriff, alle Projekte verwalten |

## Setup

### 1. .NET 10 SDK installieren
```bash
dotnet --version   # muss 10.x.x zeigen
# https://dotnet.microsoft.com/download/dotnet/10.0
```

### 2. Oracle Benutzer anlegen (als SYSDBA)
```sql
CREATE USER project_user IDENTIFIED BY deinPasswort;
GRANT CONNECT, RESOURCE, CREATE SESSION TO project_user;
GRANT UNLIMITED TABLESPACE TO project_user;
```

### 3. Connection String anpassen
In `appsettings.json`:
```json
"Default": "User Id=project_user;Password=deinPasswort;Data Source=localhost:1521/XEPDB1;"
```

### 4. NuGet-Pakete & Migration
```bash
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. Starten
```bash
dotnet run
```

| URL | Beschreibung |
|---|---|
| `https://localhost:5001` | App (Login-Seite) |
| `https://localhost:5001/pages/dashboard.html` | Dashboard |
| `https://localhost:5001/swagger` | Swagger UI |
| `https://localhost:5001/openapi/v1.json` | OpenAPI JSON |

## API-Endpunkte

| Methode | Pfad | Auth | Beschreibung |
|---|---|---|---|
| POST | /api/auth/register | ❌ | Registrierung |
| POST | /api/auth/login | ❌ | Login → JWT Token |
| GET | /api/project | ✅ | Projekte laden |
| POST | /api/project | ✅ | Projekt erstellen |
| PUT | /api/project/{id} | ✅ | Projekt bearbeiten |
| DELETE | /api/project/{id} | ✅ | Projekt löschen |
| GET | /api/projects/{id}/tasks | ✅ | Gantt-Daten laden |
| POST | /api/projects/{id}/tasks | ✅ | Aufgabe erstellen |
| PUT | /api/projects/{id}/tasks/{tid} | ✅ | Aufgabe aktualisieren |
| DELETE | /api/projects/{id}/tasks/{tid} | ✅ | Aufgabe löschen |
