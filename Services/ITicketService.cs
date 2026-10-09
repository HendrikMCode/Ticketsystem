using Ticketsystem.Models;

namespace Ticketsystem.Services;

/// <summary>Vorschläge aus bisherigen Eingaben. Ansprechpartner, Mandant und Umfeld kommen aus den Auswahllisten.</summary>
public record Vorschlaege(IReadOnlyList<string> WartetAuf);

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

    // Auswahllisten für Ansprechpartner, Mandant und Umfeld (Einstellungen)
    Task<List<Auswahlwert>> AuswahlwerteAsync();
    Task<Auswahlwert> AuswahlwertHinzufuegenAsync(AuswahlFeld feld, string wert);
    /// <summary>Benennt einen Wert um und ändert ihn auch in allen Tickets. Liefert die Zahl der geänderten Tickets.</summary>
    Task<int> AuswahlwertUmbenennenAsync(int id, string neu);
    Task AuswahlwertLoeschenAsync(int id);
    Task AuswahlwerteSortierenAsync(AuswahlFeld feld, IReadOnlyList<int> ids);

    /// <summary>Wird ausgelöst, wenn sich Tickets ändern (z. B. für den Erinnerungs-Zähler in der Navigation).</summary>
    event Action? Geaendert;
}
