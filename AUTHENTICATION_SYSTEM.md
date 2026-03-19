# Authentifizierungssystem - Projektplaner

## Überblick

Das Authentifizierungssystem wurde gemäß den Anforderungen des Lastenhefts implementiert. **Es gibt keine Selbstregistrierung** - alle Benutzerkonten werden administrativ angelegt.

## Systemablauf

### 1. Benutzer-Erstellung (nur Administrator)

- **Zugriff:** Nur Administratoren können neue Benutzer anlegen
- **Seite:** `/pages/user-management.html`
- **Standardpasswort:** `Schule2024!`
- **Prozess:**
  1. Administrator meldet sich an
  2. Navigiert zu "Benutzerverwaltung" (👥 Benutzer)
  3. Gibt Name, E-Mail und Rolle ein
  4. System erstellt Benutzer mit Standardpasswort
  5. `MustChangePassword` wird auf `true` gesetzt
  6. `PrivacyAccepted` wird auf `false` gesetzt

### 2. Erste Anmeldung (Schüler/Lehrer)

- **Login-Seite:** `/index.html`
- **Prozess:**
  1. Benutzer gibt E-Mail und Standardpasswort ein
  2. System prüft `MustChangePassword` Flag
  3. Bei erstem Login: Weiterleitung zu `/pages/change-password.html`
  4. Benutzer muss neues Passwort wählen (min. 6 Zeichen, 1 Zahl)
  5. Nach erfolgreicher Änderung: `MustChangePassword` = `false`
  6. Weiterleitung zum Dashboard

### 3. Datenschutz

- Benutzer müssen beim ersten Zugriff die Datenschutzbestimmungen akzeptieren
- Protokollierung in `PrivacyConsents` Tabelle mit:
  - `UserId`
  - `AcceptedAt` (Zeitstempel)
  - `IpAddress`
  - `Version`

## API-Endpunkte

### Entfernte Endpunkte
- ❌ `POST /api/auth/register` - **ENTFERNT** (keine Selbstregistrierung erlaubt)

### Verfügbare Endpunkte

#### `POST /api/auth/login`
Login für alle Benutzer
```json
{
  "email": "student@schule.at",
  "password": "passwort"
}
```

**Response:**
```json
{
  "token": "jwt_token",
  "mustChangePassword": true,
  "privacyAccepted": false
}
```

#### `POST /api/auth/create-user` (nur Admin)
Erstellt einen neuen Benutzer mit Standardpasswort
```json
{
  "email": "student@schule.at",
  "fullName": "Max Mustermann",
  "role": "Student"
}
```

**Response:**
```json
{
  "message": "Benutzer 'Max Mustermann' angelegt.",
  "userId": "user-id",
  "defaultPassword": "Schule2024!",
  "mustChangePassword": true
}
```

#### `POST /api/auth/change-password` (authentifiziert)
Ändert das Passwort
```json
{
  "currentPassword": "Schule2024!",
  "newPassword": "MeinNeuesPasswort123"
}
```

#### `GET /api/auth/me` (authentifiziert)
Gibt aktuelle Benutzerinformationen zurück

#### `GET /api/auth/getAll` (Admin/Teacher)
Gibt alle Benutzer zurück

#### `PUT /api/auth/setRole` (nur Admin)
Ändert die Rolle eines Benutzers
```json
{
  "userId": "user-id",
  "role": "Teacher"
}
```

## Benutzerrollen

### Admin
- Kann neue Benutzer anlegen
- Kann Benutzerrollen ändern
- Kann Projekte erstellen und verwalten
- Sieht alle Benutzer und Projekte

### Teacher (Lehrer)
- Kann keine Projekte erstellen
- Kann Schüler zu Projekten zuweisen
- Sieht alle Benutzer

### Student (Schüler)
- Sieht nur zugewiesene Projekte
- Kann keine Benutzer verwalten
- Kann keine Projekte erstellen

## Sicherheitsanforderungen

### Passwortrichtlinien
- Mindestens 6 Zeichen
- Mindestens 1 Zahl
- Wird bei erster Anmeldung erzwungen

### Standardpasswort
- `Schule2024!`
- Hart codiert in `AuthController.cs` (Zeile 209)
- Kann nur durch Administrator vergeben werden
- Muss beim ersten Login geändert werden

## Frontend-Seiten

### `/index.html`
- **Nur Login** (Registrierungs-Tab entfernt)
- Weiterleitung bei `mustChangePassword = true`

### `/pages/change-password.html`
- Passwortänderung erzwingen
- Validierung: Min. 6 Zeichen, 1 Zahl
- Weiterleitung zum Dashboard nach Erfolg

### `/pages/user-management.html`
- **Nur für Administratoren**
- Benutzer anlegen
- Benutzerrollen ändern
- Alle Benutzer anzeigen

### `/pages/dashboard.html`
- Navigation zu Benutzerverwaltung (nur für Admins sichtbar)
- Admin-Panel für Rollenverwaltung
- Teacher-Panel für Projektzuweisungen

## Änderungsprotokoll

### Entfernt
1. **Public Register-Endpunkt** (`POST /api/auth/register`)
   - Datei: `Controllers/AuthController.cs`, Zeilen 32-76
   - Grund: Keine Selbstregistrierung gemäß Lastenheft

2. **Registrierungs-Tab in Login-Seite**
   - Datei: `wwwroot/index.html`
   - Entfernte Elemente:
     - Tab-Navigation
     - Registrierungsformular
     - JavaScript-Funktionen: `register()`, `showTab()`, `toggleRegisterButton()`, `checkPrivacyBeforeRegister()`

3. **RegisterDto**
   - Datei: `Controllers/AuthController.cs`, Zeile 232
   - Nicht mehr benötigt

### Hinzugefügt
1. **Admin User Management Page** (`wwwroot/pages/user-management.html`)
   - Benutzer anlegen mit Standardpasswort
   - Benutzerrollen ändern
   - Benutzerübersicht

2. **Change Password Page** (`wwwroot/pages/change-password.html`)
   - Erzwungene Passwortänderung bei erstem Login
   - Passwortvalidierung

3. **Admin-Navigation in Dashboard**
   - Link zu Benutzerverwaltung (nur für Admins sichtbar)

## Deployment-Hinweise

### Datenbank-Migration
Keine Migration erforderlich - das bestehende Schema unterstützt bereits:
- `MustChangePassword` Flag
- `PrivacyAccepted` Flag
- `PrivacyConsents` Tabelle

### Erster Administrator
Der erste erstellte Benutzer über `create-user` sollte die Rolle "Admin" erhalten.
Alternativ kann direkt in der Datenbank ein Admin-Benutzer angelegt werden.

### Konfiguration
Standardpasswort kann in `Controllers/AuthController.cs` Zeile 209 geändert werden:
```csharp
const string defaultPassword = "Schule2024!";
```

## Testszenarien

### Szenario 1: Admin legt Schüler an
1. Admin meldet sich an
2. Navigiert zu Benutzerverwaltung
3. Legt Schüler "Max Mustermann" an
4. System zeigt Standardpasswort `Schule2024!`
5. Admin gibt Zugangsdaten an Schüler weiter

### Szenario 2: Schüler meldet sich zum ersten Mal an
1. Schüler öffnet Login-Seite
2. Gibt E-Mail und Standardpasswort ein
3. System leitet zu `/pages/change-password.html` weiter
4. Schüler wählt neues Passwort (z.B. "MeinPasswort123")
5. Nach Erfolg: Weiterleitung zum Dashboard

### Szenario 3: Lehrer bekommt Admin-Rechte
1. Admin öffnet Benutzerverwaltung
2. Klickt "Rolle ändern" bei Lehrer
3. Wählt "Administrator"
4. System aktualisiert Rolle
5. Lehrer hat ab sofort Admin-Rechte

## Compliance mit Lastenheft

✅ **MUSS-Anforderung erfüllt:**
> "Die Login-Funktionalität muss einerseits die Möglichkeit bieten, Schüler über die Schul-E-Mail-Adresse anzumelden und ein Standardkennwort zu vergeben bzw. rücksetzen zu können."

✅ **MUSS-Anforderung erfüllt:**
> "Bei der Anmeldung muss die Möglichkeit bestehen, das Standardkennwort mit einem eigenen zu ersetzen."

✅ **Keine Selbstregistrierung:**
> Accounts werden ausschließlich vom Administrator angelegt, nicht von Benutzern selbst.

## Support

Bei Fragen oder Problemen:
- Überprüfen Sie die Browser-Konsole auf Fehlermeldungen
- Prüfen Sie die Server-Logs für API-Fehler
- Stellen Sie sicher, dass der erste Benutzer Admin-Rechte hat
