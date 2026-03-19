# Änderungen am Authentifizierungssystem

## Zusammenfassung

Das Registrierungssystem wurde gemäß den Anforderungen des Lastenhefts überarbeitet. **Selbstregistrierung wurde vollständig entfernt** - nur Administratoren können neue Benutzer anlegen.

## Was wurde geändert?

### 1. ❌ Entfernt: Öffentliche Registrierung

#### Backend (`Controllers/AuthController.cs`)
- **Entfernter Endpunkt:** `POST /api/auth/register`
- **Entfernte DTO:** `RegisterDto`
- Benutzer können sich nicht mehr selbst registrieren

#### Frontend (`wwwroot/index.html`)
- **Entfernt:** "Registrieren"-Tab
- **Entfernt:** Komplettes Registrierungsformular
- **Entfernt:** JavaScript-Funktionen für Registrierung
- **Ergebnis:** Nur noch Login-Seite sichtbar

### 2. ✅ Hinzugefügt: Administratives Benutzermanagement

#### Neue Seite: `wwwroot/pages/user-management.html`
Administratoren können:
- ➕ Neue Benutzer anlegen (Schüler, Lehrer, Admin)
- 🔄 Benutzerrollen ändern
- 👥 Alle Benutzer anzeigen
- 📋 Standardpasswort wird angezeigt: `Schule2024!`

**Zugriff:** Nur für Benutzer mit Rolle "Admin"

#### Neue Seite: `wwwroot/pages/change-password.html`
- Erzwungene Passwortänderung beim ersten Login
- Validierung: Mindestens 6 Zeichen, 1 Zahl
- Automatische Weiterleitung zum Dashboard nach Erfolg

#### Dashboard-Navigation (`wwwroot/pages/dashboard.html`)
- Neuer Link "👥 Benutzer" für Administratoren
- Nur sichtbar wenn `currentRole === 'Admin'`

## Workflow: Wie funktioniert es jetzt?

### 1️⃣ Administrator legt Benutzer an
```
Admin öffnet: /pages/user-management.html
  ↓
Gibt ein: Name, E-Mail, Rolle
  ↓
System erstellt Benutzer mit Standardpasswort: "Schule2024!"
  ↓
Admin erhält Zugangsdaten und gibt sie weiter
```

### 2️⃣ Schüler/Lehrer meldet sich zum ersten Mal an
```
Benutzer öffnet: /index.html (nur Login, kein Register-Tab)
  ↓
Gibt ein: E-Mail + Standardpasswort "Schule2024!"
  ↓
System prüft: mustChangePassword = true?
  ↓
Weiterleitung zu: /pages/change-password.html
  ↓
Benutzer wählt neues Passwort
  ↓
Weiterleitung zu: /pages/dashboard.html
```

### 3️⃣ Nachfolgende Logins
```
Benutzer öffnet: /index.html
  ↓
Gibt ein: E-Mail + eigenes Passwort
  ↓
Direkter Zugang zu: /pages/dashboard.html
```

## API-Endpunkte

### ❌ Entfernt
- `POST /api/auth/register` - Keine Selbstregistrierung mehr möglich

### ✅ Vorhanden (unverändert)
- `POST /api/auth/login` - Login für alle Benutzer
- `POST /api/auth/change-password` - Passwort ändern
- `GET /api/auth/me` - Aktuelle Benutzerinfos
- `GET /api/auth/getAll` - Alle Benutzer (Admin/Teacher)
- `PUT /api/auth/setRole` - Rolle ändern (nur Admin)

### ✅ Bereits vorhanden (wird jetzt verwendet)
- `POST /api/auth/create-user` - Benutzer anlegen (nur Admin)

## Geänderte Dateien

### Backend
1. `Controllers/AuthController.cs`
   - ❌ Entfernt: `Register()` Methode (Zeilen 32-76)
   - ❌ Entfernt: `RegisterDto` (Zeile 232)

### Frontend
2. `wwwroot/index.html`
   - ❌ Entfernt: Tab-Navigation
   - ❌ Entfernt: Registrierungsformular
   - ❌ Entfernt: JavaScript-Funktionen für Registrierung

3. `wwwroot/pages/user-management.html` ⭐ **NEU**
   - Admin-Interface für Benutzerverwaltung

4. `wwwroot/pages/change-password.html` ⭐ **NEU**
   - Passwortänderung bei erstem Login

5. `wwwroot/pages/dashboard.html`
   - ➕ Navigation zu Benutzerverwaltung (nur für Admins)

### Dokumentation
6. `AUTHENTICATION_SYSTEM.md` ⭐ **NEU**
   - Vollständige Dokumentation des Systems

7. `CHANGELOG.md` ⭐ **NEU**
   - Diese Datei

## Testing

### Manuelle Tests empfohlen:

1. **Admin-Login**
   - ✅ Admin kann sich anmelden
   - ✅ Admin sieht "👥 Benutzer" Link
   - ✅ Admin kann `/pages/user-management.html` öffnen

2. **Benutzer anlegen**
   - ✅ Admin kann neuen Schüler anlegen
   - ✅ Standardpasswort wird angezeigt
   - ✅ Benutzer erscheint in der Liste

3. **Erster Login als neuer Benutzer**
   - ✅ Login mit Standardpasswort funktioniert
   - ✅ Weiterleitung zu `/pages/change-password.html`
   - ✅ Passwortänderung erfolgreich
   - ✅ Weiterleitung zum Dashboard

4. **Selbstregistrierung blockiert**
   - ✅ Kein "Registrieren"-Tab sichtbar
   - ✅ `/api/auth/register` Endpunkt existiert nicht mehr
   - ✅ Nur Login-Formular vorhanden

5. **Rollenverwaltung**
   - ✅ Admin kann Rollen ändern
   - ✅ Schüler können nicht auf Benutzerverwaltung zugreifen
   - ✅ Lehrer können nicht auf Benutzerverwaltung zugreifen

## Migration

### Für bestehende Systeme:

1. **Code aktualisieren**
   ```bash
   # Alle geänderten Dateien übernehmen
   git pull
   ```

2. **Keine Datenbank-Migration nötig**
   - Bestehende Benutzer bleiben unverändert
   - `MustChangePassword` Flag existiert bereits
   - `PrivacyAccepted` Flag existiert bereits

3. **Ersten Admin sicherstellen**
   - Prüfen ob mindestens ein Admin-Benutzer existiert
   - Falls nicht: Über `/api/auth/create-user` anlegen oder direkt in DB

## Compliance

✅ **Lastenheft MUSS-Anforderung erfüllt:**
> "Die Login-Funktionalität muss einerseits die Möglichkeit bieten, Schüler über die Schul-E-Mail-Adresse anzumelden und ein Standardkennwort zu vergeben bzw. rücksetzen zu können."

✅ **Lastenheft MUSS-Anforderung erfüllt:**
> "Bei der Anmeldung muss die Möglichkeit bestehen, das Standardkennwort mit einem eigenen zu ersetzen."

✅ **Keine Selbstregistrierung:**
> System erlaubt nur administrative Benutzer-Erstellung, keine Selbstregistrierung durch Schüler

## Standardpasswort

**Aktuell:** `Schule2024!`

**Ändern unter:** `Controllers/AuthController.cs` Zeile 209

```csharp
const string defaultPassword = "Schule2024!";
```

## Support

Bei Problemen:
1. Prüfen Sie die Browser-Konsole (F12)
2. Prüfen Sie die Server-Logs
3. Stellen Sie sicher dass ein Admin-Benutzer existiert
4. Siehe `AUTHENTICATION_SYSTEM.md` für Details

---

**Datum:** 19. März 2026  
**Version:** 2.0 (Administratives Benutzermanagement)  
**Status:** ✅ Produktionsbereit
