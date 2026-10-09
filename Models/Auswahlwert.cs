using System.ComponentModel.DataAnnotations;

namespace Ticketsystem.Models;

/// <summary>Felder im Ticket, deren Werte aus einer festen Auswahl (Einstellungen) kommen.</summary>
public enum AuswahlFeld
{
    Ansprechpartner = 0,
    Mandant = 1,
    Umfeld = 2,
}

/// <summary>Ein Eintrag der Auswahlliste, z. B. ein Mandant. Wird in den Einstellungen gepflegt.</summary>
public class Auswahlwert
{
    public int Id { get; set; }
    public AuswahlFeld Feld { get; set; }
    [MaxLength(200)] public string Wert { get; set; } = "";
    public int Reihenfolge { get; set; }
}

public static class Auswahl
{
    /// <summary>Trennzeichen, wenn im Ticket mehrere Werte ausgewählt sind (Komma geht nicht, z. B. „Müller, Hans“).</summary>
    public const string Trenner = "; ";

    public static List<string> Trennen(string? wert) =>
        string.IsNullOrWhiteSpace(wert) ? [] :
        wert.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    public static string? Verbinden(IEnumerable<string> werte)
    {
        var text = string.Join(Trenner, werte);
        return text.Length == 0 ? null : text;
    }

    /// <summary>Für die Anzeige: mehrere Werte mit Komma getrennt.</summary>
    public static string Anzeige(string? wert) => string.Join(", ", Trennen(wert));

    public static string Name(this AuswahlFeld f) => f switch
    {
        AuswahlFeld.Ansprechpartner => "Ansprechpartner",
        AuswahlFeld.Mandant => "Mandanten",
        AuswahlFeld.Umfeld => "Umfelder",
        _ => f.ToString(),
    };

    public static string Symbol(this AuswahlFeld f) => f switch
    {
        AuswahlFeld.Ansprechpartner => "person",
        AuswahlFeld.Mandant => "apartment",
        AuswahlFeld.Umfeld => "dns",
        _ => "list",
    };

    public static string? WertIn(this AuswahlFeld f, Ticket t) => f switch
    {
        AuswahlFeld.Ansprechpartner => t.Ansprechpartner,
        AuswahlFeld.Mandant => t.Mandant,
        AuswahlFeld.Umfeld => t.Umfeld,
        _ => null,
    };

    public static void SetzenIn(this AuswahlFeld f, Ticket t, string? wert)
    {
        switch (f)
        {
            case AuswahlFeld.Ansprechpartner: t.Ansprechpartner = wert; break;
            case AuswahlFeld.Mandant: t.Mandant = wert; break;
            case AuswahlFeld.Umfeld: t.Umfeld = wert; break;
        }
    }
}
