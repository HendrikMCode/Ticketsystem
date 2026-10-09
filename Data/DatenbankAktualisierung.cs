using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Ticketsystem.Models;

namespace Ticketsystem.Data;

/// <summary>
/// Bringt eine bestehende Datenbank auf den aktuellen Stand, ohne Daten zu verlieren.
/// Neue Tabellen werden ergänzt; vorher wird eine Sicherungskopie unter Daten/Backups angelegt.
/// </summary>
public static class DatenbankAktualisierung
{
    public static void Ausfuehren(TicketDbContext db, string datenOrdner, ILogger logger)
    {
        // Neue Datenbank: legt alle Tabellen an. Bestehende Datenbank: macht nichts.
        var neueDatenbank = db.Database.EnsureCreated();
        var auswahlFuellen = neueDatenbank;

        var verbindung = db.Database.GetDbConnection();
        verbindung.Open();
        try
        {
            if (!TabelleVorhanden(verbindung, "Aufgaben"))
            {
                Sichern(verbindung, datenOrdner, "vor-tasks", logger);
                Ausfuehren(verbindung, """
                    CREATE TABLE IF NOT EXISTS "Aufgaben" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_Aufgaben" PRIMARY KEY AUTOINCREMENT,
                        "TicketId" INTEGER NOT NULL,
                        "Titel" TEXT NOT NULL,
                        "Status" TEXT NOT NULL,
                        "RestaufwandStunden" REAL NULL,
                        "Reihenfolge" INTEGER NOT NULL,
                        "ErstelltAm" TEXT NOT NULL,
                        "ErledigtAm" TEXT NULL,
                        CONSTRAINT "FK_Aufgaben_Tickets_TicketId" FOREIGN KEY ("TicketId") REFERENCES "Tickets" ("Id") ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS "IX_Aufgaben_TicketId" ON "Aufgaben" ("TicketId");
                    """);
                logger.LogInformation("Datenbank erweitert: Tabelle „Aufgaben“ angelegt.");
            }

            if (!TabelleVorhanden(verbindung, "Bilder"))
            {
                Sichern(verbindung, datenOrdner, "vor-bilder", logger);
                Ausfuehren(verbindung, """
                    CREATE TABLE IF NOT EXISTS "Bilder" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_Bilder" PRIMARY KEY AUTOINCREMENT,
                        "TicketId" INTEGER NOT NULL,
                        "Dateiname" TEXT NOT NULL,
                        "ContentType" TEXT NOT NULL,
                        "Daten" BLOB NOT NULL,
                        "Groesse" INTEGER NOT NULL,
                        "ErstelltAm" TEXT NOT NULL,
                        CONSTRAINT "FK_Bilder_Tickets_TicketId" FOREIGN KEY ("TicketId") REFERENCES "Tickets" ("Id") ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS "IX_Bilder_TicketId" ON "Bilder" ("TicketId");
                    """);
                logger.LogInformation("Datenbank erweitert: Tabelle „Bilder“ angelegt.");
            }

            if (!TabelleVorhanden(verbindung, "Auswahlwerte"))
            {
                Sichern(verbindung, datenOrdner, "vor-auswahlwerte", logger);
                Ausfuehren(verbindung, """
                    CREATE TABLE IF NOT EXISTS "Auswahlwerte" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_Auswahlwerte" PRIMARY KEY AUTOINCREMENT,
                        "Feld" TEXT NOT NULL,
                        "Wert" TEXT NOT NULL,
                        "Reihenfolge" INTEGER NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS "IX_Auswahlwerte_Feld" ON "Auswahlwerte" ("Feld");
                    """);
                logger.LogInformation("Datenbank erweitert: Tabelle „Auswahlwerte“ angelegt.");
                auswahlFuellen = true;
            }
        }
        finally
        {
            verbindung.Close();
        }

        if (auswahlFuellen) AuswahlFuellen(db, logger);
    }

    /// <summary>
    /// Erste Befüllung der Auswahllisten: alle bisher in Tickets eingetragenen Werte übernehmen,
    /// damit man in den Einstellungen nicht bei null anfängt. Bei Umfeldern ein paar übliche Werte dazu.
    /// </summary>
    private static void AuswahlFuellen(TicketDbContext db, ILogger logger)
    {
        var tickets = db.Tickets.AsNoTracking().Select(t => new { t.Ansprechpartner, t.Mandant, t.Umfeld }).ToList();
        (AuswahlFeld Feld, IEnumerable<string?> Werte, string[] Standard)[] quellen =
        [
            (AuswahlFeld.Ansprechpartner, tickets.Select(t => t.Ansprechpartner), []),
            (AuswahlFeld.Mandant, tickets.Select(t => t.Mandant), []),
            (AuswahlFeld.Umfeld, tickets.Select(t => t.Umfeld), ["Entwicklung", "Test", "QS", "Produktion"]),
        ];

        foreach (var (feld, werte, standard) in quellen)
        {
            var liste = standard
                .Concat(werte.SelectMany(Auswahl.Trennen).Order(StringComparer.CurrentCultureIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            for (var i = 0; i < liste.Count; i++)
                db.Auswahlwerte.Add(new Auswahlwert { Feld = feld, Wert = liste[i], Reihenfolge = i + 1 });
        }
        db.SaveChanges();
        logger.LogInformation("Auswahllisten aus den vorhandenen Tickets befüllt.");
    }

    private static bool TabelleVorhanden(DbConnection verbindung, string name)
    {
        using var cmd = verbindung.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name";
        var p = cmd.CreateParameter();
        p.ParameterName = "$name";
        p.Value = name;
        cmd.Parameters.Add(p);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    /// <summary>Konsistente Kopie der Datenbank (SQLite „VACUUM INTO“).</summary>
    private static void Sichern(DbConnection verbindung, string datenOrdner, string anlass, ILogger logger)
    {
        var ordner = Path.Combine(datenOrdner, "Backups");
        Directory.CreateDirectory(ordner);
        var ziel = Path.Combine(ordner, $"tickets-{anlass}-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.db");
        Ausfuehren(verbindung, $"VACUUM INTO '{ziel.Replace("'", "''")}'");
        logger.LogInformation("Sicherungskopie angelegt: {Ziel}", ziel);
    }

    private static void Ausfuehren(DbConnection verbindung, string sql)
    {
        using var cmd = verbindung.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
