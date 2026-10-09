using System.Data.Common;
using Microsoft.EntityFrameworkCore;

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
        db.Database.EnsureCreated();

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
        }
        finally
        {
            verbindung.Close();
        }
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
