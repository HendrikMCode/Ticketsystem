# Ticketsystem

Ein eigenes Ticketsystem für die Entwicklung, gebaut mit **C# / .NET 10, Blazor (Razor-Komponenten) und SQLite**.
Die Oberfläche nutzt **Material Design 3** über [Material Web](https://material-web.dev) (`@material/web` 2.5.0), dazu die Schrift Google Sans Flex und Material Symbols von Google Fonts.
Es läuft komplett lokal auf deinem Rechner, ohne Server und ohne Datenbank-Installation.

## Starten

1. **Einmalig:** das [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) installieren
   (oder in der Eingabeaufforderung: `winget install Microsoft.DotNet.SDK.10`).
2. **`start.bat` doppelklicken** (Windows), bzw. `./start.sh` (macOS/Linux).
3. Der Browser öffnet sich automatisch auf **http://localhost:5080**.

Beim allerersten Start lädt .NET einmalig die benötigten Pakete aus dem Internet.
Die Material-Komponenten, Schriften und Symbole lädt der Browser von jsDelivr und Google Fonts. Dafür braucht er Internet, sonst fehlen Buttons und Icons.
Zum Beenden das Konsolenfenster schließen.

Alternativ kannst du `Ticketsystem.csproj` in **Visual Studio** oder **Rider** öffnen und mit F5 starten.

## Daten

Alle Tickets liegen in **`Daten/tickets.db`** (SQLite) im Projektordner. Die Datei wird beim ersten Start automatisch angelegt.
**Backup** = diese eine Datei kopieren. Anschauen kannst du sie z. B. mit dem [DB Browser for SQLite](https://sqlitebrowser.org/).

## Funktionen

| Bereich | Felder |
|---|---|
| Kopf | Ticketnummer (automatisch `DEV-0001`, `DEV-0002` …, frei änderbar), Betreff |
| Allgemein | Ansprechpartner, Mandant, Umfeld (ein oder mehrere Einträge aus den Auswahllisten unter **Einstellungen**), Feature-Branch |
| Zeitangaben | Aufgabe erhalten, Erinnerung (mit Schnellwahl), Fälligkeitsdatum, Erledigt am |
| Status | Status, Wartet auf, Wartegrund, Wartet seit |
| Tests | Lokal-Test, Consultant-Test |
| PR-Verwaltung | PR gestellt, Keine Task mehr |
| Erledigt | Erledigt |
| Beschreibung | Freitext |
| Schritte | Datierte Notizen als Zeitleiste (Strg+Enter zum Hinzufügen) |

**Board & Tasks (wie in Azure DevOps):**
- **Board:** Kanban mit einer Spalte pro Status – Tickets per Drag & Drop verschieben (legt automatisch einen Status-Schritt an). „Erledigt“ zeigt standardmäßig die letzten 14 Tage.
- **Taskboard:** pro Ticket eine Zeile, Tasks in den Spalten Zu erledigen / In Arbeit / Erledigt. Ziehen oder auf das Symbol klicken, Doppelklick zum Bearbeiten, „+ Task“ zum Anlegen.
- **Tasks im Ticket:** Titel, Status, Restaufwand (Stunden), per Ziehen sortierbar. „Keine Task mehr“ geht automatisch an, wenn alle Tasks erledigt sind.

**Formatierung & Statistik:**
- Beschreibung und Schritte unterstützen **Markdown**: `# Überschrift`, `**fett**`, `*kursiv*`, `~~durch~~`, `` `Code` ``, Codeblöcke mit Sprache (```` ```sql ````), Listen, Checklisten (`- [ ]`), Zitate (`>`), Tabellen und Links. Codeblöcke werden eingefärbt (highlight.js) und haben einen Kopier-Knopf. Werkzeugleiste und Strg+B/I/E/K im Bearbeiten-Modus.
- **Statistik:** Kennzahlen (offen, erledigt, neu, Durchlaufzeit, Tasks), Neu/Erledigt pro Woche bzw. Monat, offene Tickets nach Status, Ø Tage je Status (aus den Status-Schritten), Verteilung der Durchlaufzeit, Tickets je Mandant und die am längsten wartenden Tickets – wählbar für 30 Tage, 90 Tage, 12 Monate oder gesamt.

**Bilder & Bearbeiten-Modus:**
- Bilder (z. B. Screenshots) in Beschreibung und Schritten: **Strg+V**, Drag & Drop oder „Bild einfügen“. Sie werden in der Datenbank gespeichert (Tabelle „Bilder“) und im Text als `![Name](bilder/ID)` referenziert. Links im Text sind klickbar.
- Bei bestehenden Tickets sind Titel, Nummer, Ansprechpartner, Mandant, Umfeld, Feature-Branch, Beschreibung und alle Datumsangaben **geschützt** – erst „Bearbeiten“ (oben) bzw. das Stift-Symbol am Abschnitt macht sie änderbar. Neue Tickets sind komplett offen.
- Feature-Branch lässt sich per Klick kopieren.

**Automatiken:**
- **Auto-Speichern:** Jede Änderung wird nach 1 Sekunde gespeichert (Anzeige oben rechts wie in Google Docs). Neue Tickets werden angelegt, sobald ein Betreff eingetragen ist. Beim Verlassen der Seite wird vorher gespeichert.
- **Datumsfilter** in der Übersicht: Erhalten, Fällig, Erinnerung, Erledigt am, Geändert – Schnellauswahl (Heute, Diese Woche, Letzte 7 Tage …) oder eigener Zeitraum. Filter stehen in der Adresse und lassen sich als Lesezeichen speichern.
- Eigene Schritte lassen sich nachträglich bearbeiten (Text und Datum), automatisch erzeugte Status-Schritte nicht.
- Status „Wartend“ setzt „Wartet seit“ automatisch auf jetzt.
- „Erledigt“ setzt den Status auf *Erledigt* und füllt „Erledigt am“.
- Überfällige Tickets und fällige Erinnerungen werden in der Übersicht hervorgehoben und oben einsortiert.
  In der Seitenleiste zählt ein Badge die fälligen Erinnerungen (wird jede Minute aktualisiert).
- Hell- und Dunkelmodus (Mond/Sonne oben rechts).
- Suche oben in der Leiste: durchsucht alle Felder und Schritte.

## Projektstruktur

```
Models/        Ticket, Schritt, Aufgabe (Task), TicketStatus, DatumsFilter, TicketSuche
Data/          TicketDbContext (EF Core), DatenbankAktualisierung (Schema-Ergänzung + Backup)
Services/      TicketService – Laden, Speichern, Löschen
Components/
  Pages/       Uebersicht (Liste), Board (Kanban), Taskboard, Statistik, TicketDetail (Bearbeiten)
  Layout/      MainLayout.razor (Seitenleiste)
  Shared/      MdText, MdDate, MdSwitch (Material-Eingabefelder), Icon, StatusBadge
wwwroot/       app.css (Material-3-Farben, Layout, Komponenten-Anpassungen)
Program.cs     Start, Datenbank anlegen, Browser öffnen
```

## Einstellungen (`appsettings.json`)

- `"Urls": "http://localhost:5080"` → Port ändern, falls 5080 belegt ist.
- `"BrowserOeffnen": false` → Browser nicht automatisch öffnen.

## Datenbank-Aktualisierung und Sicherungen

Beim Start prüft die App, ob neue Tabellen fehlen (`Data/DatenbankAktualisierung.cs`), und ergänzt sie – vorhandene Tickets bleiben erhalten.
Vor jeder solchen Änderung wird automatisch eine Kopie nach `Daten/Backups/` geschrieben.

## Hinweis zu späteren Änderungen am Datenmodell

Die Datenbank wird per `EnsureCreated()` angelegt. Wenn du später **neue Felder** hinzufügst, gibt es zwei Wege:
- **Einfach:** `Daten/tickets.db` löschen, dann wird sie neu erzeugt. Achtung: Dabei gehen alle Tickets verloren.
- **Sauber:** auf EF-Core-Migrationen umstellen (`dotnet tool install -g dotnet-ef`, dann `dotnet ef migrations add Initial`
  und in `Program.cs` `EnsureCreated()` durch `Migrate()` ersetzen).

## Betrieb mit Docker / Rancher Desktop

Im Projektordner (neben der `.csproj`):

| Aktion | Befehl |
|---|---|
| Erststart / nach Code-Änderungen | `docker compose up -d --build` |
| Backup der Datenbank | `docker compose cp ticketsystem:/app/Daten/tickets.db ./backup-tickets.db` |
| Logs ansehen | `docker compose logs -f` |
| Anhalten | `docker compose stop` |

Der Container läuft mit `restart: unless-stopped`. Er startet also automatisch mit Rancher Desktop (Engine: dockerd/moby, Autostart beim Login aktiv).
Die Datenbank liegt im Docker-Volume `ticket-daten`, die Zeitzone ist auf `Europe/Berlin` gesetzt, und die App ist nur lokal unter http://localhost:5080 erreichbar.
