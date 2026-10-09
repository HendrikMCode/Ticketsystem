namespace Ticketsystem.Models;

public enum TicketStatus
{
    Neu = 0,
    InArbeit = 1,
    Wartend = 2,
    ImTest = 3,
    ImReview = 4,
    Erledigt = 5,
}

public static class TicketStatusExtensions
{
    public static string Anzeige(this TicketStatus s) => s switch
    {
        TicketStatus.Neu => "Neu",
        TicketStatus.InArbeit => "In Arbeit",
        TicketStatus.Wartend => "Wartend",
        TicketStatus.ImTest => "Im Test",
        TicketStatus.ImReview => "Im Review",
        TicketStatus.Erledigt => "Erledigt",
        _ => s.ToString(),
    };

    /// <summary>Material-Symbol (Google Fonts) für den Status.</summary>
    public static string Symbol(this TicketStatus s) => s switch
    {
        TicketStatus.Neu => "fiber_new",
        TicketStatus.InArbeit => "code",
        TicketStatus.Wartend => "hourglass_top",
        TicketStatus.ImTest => "science",
        TicketStatus.ImReview => "rate_review",
        TicketStatus.Erledigt => "task_alt",
        _ => "label",
    };

    /// <summary>Anfang des Textes automatisch erzeugter Status-Schritte.</summary>
    public const string SchrittPraefix = "Status geändert:";

    public static string SchrittText(TicketStatus von, TicketStatus nach) =>
        $"{SchrittPraefix} {von.Anzeige()} → {nach.Anzeige()}";

    /// <summary>Liest „Status geändert: A → B“ aus einem Schritt-Text.</summary>
    public static bool SchrittLesen(string? text, out TicketStatus von, out TicketStatus nach)
    {
        von = nach = TicketStatus.Neu;
        if (text is null || !text.StartsWith(SchrittPraefix)) return false;
        var zeile = text.Split('\n')[0][SchrittPraefix.Length..];
        var teile = zeile.Split('→');
        if (teile.Length != 2) return false;
        TicketStatus? Finde(string name)
        {
            foreach (var st in Enum.GetValues<TicketStatus>())
                if (st.Anzeige() == name.Trim()) return st;
            return null;
        }
        if (Finde(teile[0]) is not { } v || Finde(teile[1]) is not { } n) return false;
        von = v;
        nach = n;
        return true;
    }

    /// <summary>CSS-Klasse für die Status-Farbe.</summary>
    public static string Css(this TicketStatus s) => "st-" + s.ToString().ToLowerInvariant();
}
