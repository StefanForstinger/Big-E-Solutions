# Big-E-Solutions - ProjectPlanner

Eine moderne Projektmanagement-Webanwendung entwickelt mit ASP.NET Core.

**🌐 Live-Website:** https://projectplanner-cjcvhqc0creuhcc0.westeurope-01.azurewebsites.net/

## 📋 Über das Projekt

Big-E-Solutions ProjectPlanner ist eine webbasierte Anwendung zur effizienten Verwaltung von Projekten. Die Anwendung ist online verfügbar und bietet eine intuitive Benutzeroberfläche für die Planung, Verfolgung und Organisation von Projekten und Aufgaben.

## 🚀 Technologie-Stack

- **Backend:** ASP.NET Core (.NET 10.0)
- **Frontend:** HTML, CSS, JavaScript
- **Architektur:** MVC (Model-View-Controller)
- **Datenbank:** Oracle Database (selbst gehostet) mit Entity Framework Core
- **Hosting:** Azure App Service
- **CI/CD:** GitHub Actions

## ✨ Features

- Projektverwaltung
- Benutzerfreundliche Weboberfläche
- Datenbankgestützte Persistenz
- Responsive Design
- Service-orientierte Architektur

## 🌐 Website nutzen

Die Anwendung ist bereits live und kann direkt genutzt werden:

**👉 ProjectPlanner öffnen:** https://projectplanner-cjcvhqc0creuhcc0.westeurope-01.azurewebsites.net/

Keine Installation erforderlich - einfach die Website besuchen und loslegen!

---

## 💻 Für Entwickler

Die folgenden Abschnitte sind für Entwickler gedacht, die lokal am Projekt arbeiten oder eine eigene Instanz hosten möchten.

## 📦 Voraussetzungen

Bevor Sie beginnen, stellen Sie sicher, dass folgende Software installiert ist:

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) oder höher
- Ein Code-Editor (z.B. [Visual Studio](https://visualstudio.microsoft.com/), [VS Code](https://code.visualstudio.com/), oder [Rider](https://www.jetbrains.com/rider/))
- **Oracle Database** (für lokale Entwicklung oder eigene Instanz)
- Oracle Data Provider für .NET (wird über NuGet wiederhergestellt)

## 🛠️ Installation

### 1. Repository klonen

```bash
git clone https://github.com/StefanForstinger/Big-E-Solutions.git
cd Big-E-Solutions
```

### 2. Abhängigkeiten wiederherstellen

```bash
dotnet restore
```

### 3. Datenbank-Migrationen ausführen

```bash
dotnet ef database update
```

Oder falls die EF Tools noch nicht installiert sind:

```bash
dotnet tool install --global dotnet-ef
dotnet ef database update
```

### 4. Anwendung starten

```bash
dotnet run
```

Die Anwendung ist nun unter `https://localhost:5001` (oder `http://localhost:5000`) erreichbar.

## 🔧 Konfiguration

Die Anwendungskonfiguration befindet sich in der Datei `appsettings.json`. Hier können Sie folgende Einstellungen anpassen:

- Oracle Datenbankverbindungsstring
- Logging-Level
- Weitere anwendungsspezifische Einstellungen

**Beispiel für Oracle Connection String:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "User Id=username;Password=password;Data Source=hostname:port/servicename"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

**Hinweis:** Für die Produktion wird eine selbst gehostete Oracle Database verwendet. Bei lokaler Entwicklung stellen Sie sicher, dass Sie Zugriff auf eine Oracle Database-Instanz haben.

## 📁 Projektstruktur

```
Big-E-Solutions/
├── Controllers/        # MVC Controller
├── Models/            # Datenmodelle
├── Services/          # Business-Logik-Services
├── Data/              # Datenbank-Kontext und Konfiguration
├── Migrations/        # Entity Framework Migrationen
├── wwwroot/           # Statische Dateien (CSS, JS, Bilder)
├── Properties/        # Projekt-Properties
└── Program.cs         # Einstiegspunkt der Anwendung
```

## 🧪 Tests ausführen

```bash
dotnet test
```

## 📝 Entwicklung

### Neue Migration erstellen

```bash
dotnet ef migrations add NeuerMigrationName
```

### Datenbank zurücksetzen

```bash
dotnet ef database drop
dotnet ef database update
```

## 🚢 Deployment

Die Anwendung wird auf **Azure App Service** gehostet und automatisch über GitHub Actions deployt.

### Automatisches Deployment

Jeder Push auf den `main` Branch triggert automatisch:
1. Build der Anwendung
2. Ausführen von Tests
3. Deployment auf Azure

### Manuelles Deployment auf Azure

```bash
# Build für Production
dotnet publish -c Release -o ./publish

# Deployment über Azure CLI (falls installiert)
az webapp deploy --resource-group <ResourceGroup> --name <AppName> --src-path ./publish
```

### Alternative Hosting-Optionen

Die Anwendung kann auch auf anderen Plattformen deployt werden:
- **IIS**
- **Docker**
- **Andere .NET-kompatible Hosting-Plattformen**

## 🤝 Mitwirken

Beiträge sind willkommen! So können Sie zum Projekt beitragen:

1. Forken Sie das Repository
2. Erstellen Sie einen Feature-Branch (`git checkout -b feature/NeuesFeature`)
3. Committen Sie Ihre Änderungen (`git commit -m 'Füge neues Feature hinzu'`)
4. Pushen Sie zum Branch (`git push origin feature/NeuesFeature`)
5. Öffnen Sie einen Pull Request

## 📄 Lizenz

Weitere Informationen zur Lizenzierung entnehmen Sie bitte der LICENSE-Datei im Repository.

## 👤 Autor

**Stefan Forstinger**

- GitHub: [@StefanForstinger](https://github.com/StefanForstinger)

## 📞 Support

Bei Fragen oder Problemen öffnen Sie bitte ein [Issue](https://github.com/StefanForstinger/Big-E-Solutions/issues) im GitHub-Repository.

---

⭐ Wenn Ihnen dieses Projekt gefällt, geben Sie ihm einen Stern auf GitHub!
