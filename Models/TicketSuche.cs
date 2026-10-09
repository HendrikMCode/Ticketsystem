namespace Ticketsystem.Models;

/// <summary>Gemeinsame Volltextsuche für Übersicht, Board und Taskboard.</summary>
public static class TicketSuche
{
    public static bool Passt(Ticket t, string? suche)
    {
        if (string.IsNullOrWhiteSpace(suche)) return true;
        var s = suche.Trim();
        bool Hat(string? v) => v?.Contains(s, StringComparison.CurrentCultureIgnoreCase) == true;
        return Hat(t.Ticketnummer) || Hat(t.Betreff) || Hat(t.Mandant) || Hat(t.Ansprechpartner)
            || Hat(t.Umfeld) || Hat(t.FeatureBranch) || Hat(t.Beschreibung) || Hat(t.WartetAuf)
            || t.Schritte.Any(x => Hat(x.Text))
            || t.Aufgaben.Any(a => Hat(a.Titel));
    }
}
