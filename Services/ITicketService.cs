using Ticketsystem.Models;

namespace Ticketsystem.Services;

public record Vorschlaege(
    IReadOnlyList<string> Mandanten,
    IReadOnlyList<string> Ansprechpartner,
    IReadOnlyList<string> Umfelder,
    IReadOnlyList<string> WartetAuf);

public interface ITicketService
{
    Task<List<Ticket>> AlleAsync();
    Task<Ticket?> LadenAsync(int id);
    Task<Ticket> SpeichernAsync(Ticket ticket);
    Task LoeschenAsync(int id);
    Task<bool> NummerVergebenAsync(string nummer, int ausserId);
    Task<string> NaechsteNummerAsync();
    Task<Vorschlaege> VorschlaegeAsync();

    Task<Schritt> SchrittHinzufuegenAsync(int ticketId, DateTime datum, string text);
    Task SchrittLoeschenAsync(int schrittId);
    Task SchrittAendernAsync(int schrittId, DateTime datum, string text);

    /// <summary>Status ändern (z. B. im Board) – legt automatisch einen Status-Schritt an.</summary>
    Task StatusAendernAsync(int ticketId, TicketStatus neu);

    // Tasks – die Methoden liefern den neuen Wert von „Keine Task mehr“ zurück.
    Task<(Aufgabe Aufgabe, bool KeineTaskMehr)> AufgabeHinzufuegenAsync(int ticketId, string titel, AufgabeStatus status = AufgabeStatus.Offen);
    Task<bool> AufgabeAendernAsync(Aufgabe aufgabe);
    Task<bool> AufgabeLoeschenAsync(int aufgabeId);
    Task AufgabenSortierenAsync(int ticketId, IReadOnlyList<int> aufgabenIds);

    // Bilder (Screenshots in Beschreibung und Schritten)
    Task<int> BildSpeichernAsync(int ticketId, string dateiname, string contentType, byte[] daten);
    Task<Bild?> BildLadenAsync(int id);

    /// <summary>Wird ausgelöst, wenn sich Tickets ändern (z. B. für den Erinnerungs-Zähler in der Navigation).</summary>
    event Action? Geaendert;
}
