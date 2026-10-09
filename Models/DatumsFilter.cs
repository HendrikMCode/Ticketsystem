using System.Globalization;

namespace Ticketsystem.Models;

/// <summary>Nach welchem Datum eines Tickets gefiltert wird.</summary>
public enum DatumsFeld
{
    Erhalten,
    Faellig,
    Erinnerung,
    Erledigt,
    Geaendert,
}

/// <summary>Schnellauswahl für einen Zeitraum.</summary>
public enum DatumsPreset
{
    Heute,
    DieseWoche,
    DieserMonat,
    Letzte7,
    Letzte30,
    Naechste7,
    KeinDatum,
    Zeitraum,   // eigener Zeitraum von/bis
}

/// <summary>
/// Ein Datumsfilter, z. B. „Erhalten: letzte 7 Tage“ oder „Fällig: 01.10.–31.10.“.
/// Er steht in der Adresse als Query-Parameter, z. B. <c>?erhalten=7t</c> oder <c>?faellig=2026-10-01..2026-10-31</c>.
/// </summary>
public sealed record DatumsFilter(DatumsFeld Feld, DatumsPreset Preset, DateTime? Von = null, DateTime? Bis = null)
{
    // ── Feld-Infos ──────────────────────────────────────────────
    public static string Schluessel(DatumsFeld f) => f switch
    {
        DatumsFeld.Erhalten => "erhalten",
        DatumsFeld.Faellig => "faellig",
        DatumsFeld.Erinnerung => "erinnerung",
        DatumsFeld.Erledigt => "erledigt",
        DatumsFeld.Geaendert => "geaendert",
        _ => f.ToString().ToLowerInvariant(),
    };

    public static string Anzeige(DatumsFeld f) => f switch
    {
        DatumsFeld.Erhalten => "Erhalten",
        DatumsFeld.Faellig => "Fällig",
        DatumsFeld.Erinnerung => "Erinnerung",
        DatumsFeld.Erledigt => "Erledigt am",
        DatumsFeld.Geaendert => "Geändert",
        _ => f.ToString(),
    };

    public static string Symbol(DatumsFeld f) => f switch
    {
        DatumsFeld.Erhalten => "move_to_inbox",
        DatumsFeld.Faellig => "event",
        DatumsFeld.Erinnerung => "notifications",
        DatumsFeld.Erledigt => "task_alt",
        DatumsFeld.Geaendert => "edit_calendar",
        _ => "calendar_today",
    };

    public static DateTime? Wert(Ticket t, DatumsFeld f) => f switch
    {
        DatumsFeld.Erhalten => t.AufgabeErhalten,
        DatumsFeld.Faellig => t.Faellig,
        DatumsFeld.Erinnerung => t.Erinnerung,
        DatumsFeld.Erledigt => t.ErledigtAm,
        DatumsFeld.Geaendert => t.GeaendertAm,
        _ => null,
    };

    /// <summary>Welche Schnellauswahlen für ein Feld sinnvoll sind.</summary>
    public static IReadOnlyList<DatumsPreset> Presets(DatumsFeld f) => f switch
    {
        DatumsFeld.Faellig or DatumsFeld.Erinnerung =>
            [DatumsPreset.Heute, DatumsPreset.Naechste7, DatumsPreset.DieseWoche, DatumsPreset.DieserMonat, DatumsPreset.Letzte7, DatumsPreset.KeinDatum],
        DatumsFeld.Geaendert =>
            [DatumsPreset.Heute, DatumsPreset.Letzte7, DatumsPreset.Letzte30, DatumsPreset.DieseWoche, DatumsPreset.DieserMonat],
        _ =>
            [DatumsPreset.Heute, DatumsPreset.Letzte7, DatumsPreset.Letzte30, DatumsPreset.DieseWoche, DatumsPreset.DieserMonat, DatumsPreset.KeinDatum],
    };

    public static string Anzeige(DatumsPreset p) => p switch
    {
        DatumsPreset.Heute => "Heute",
        DatumsPreset.DieseWoche => "Diese Woche",
        DatumsPreset.DieserMonat => "Dieser Monat",
        DatumsPreset.Letzte7 => "Letzte 7 Tage",
        DatumsPreset.Letzte30 => "Letzte 30 Tage",
        DatumsPreset.Naechste7 => "Nächste 7 Tage",
        DatumsPreset.KeinDatum => "Ohne Datum",
        DatumsPreset.Zeitraum => "Eigener Zeitraum",
        _ => p.ToString(),
    };

    private static readonly Dictionary<DatumsPreset, string> Kurz = new()
    {
        [DatumsPreset.Heute] = "heute",
        [DatumsPreset.DieseWoche] = "woche",
        [DatumsPreset.DieserMonat] = "monat",
        [DatumsPreset.Letzte7] = "7t",
        [DatumsPreset.Letzte30] = "30t",
        [DatumsPreset.Naechste7] = "n7t",
        [DatumsPreset.KeinDatum] = "leer",
    };

    // ── Zeitraum berechnen (inklusive, ganze Tage) ──────────────
    public (DateTime? Von, DateTime? Bis) Bereich(DateTime heute)
    {
        heute = heute.Date;
        var montag = heute.AddDays(-(((int)heute.DayOfWeek + 6) % 7));
        var ersterImMonat = new DateTime(heute.Year, heute.Month, 1);
        return Preset switch
        {
            DatumsPreset.Heute => (heute, heute),
            DatumsPreset.DieseWoche => (montag, montag.AddDays(6)),
            DatumsPreset.DieserMonat => (ersterImMonat, ersterImMonat.AddMonths(1).AddDays(-1)),
            DatumsPreset.Letzte7 => (heute.AddDays(-6), heute),
            DatumsPreset.Letzte30 => (heute.AddDays(-29), heute),
            DatumsPreset.Naechste7 => (heute, heute.AddDays(7)),
            DatumsPreset.Zeitraum => (Von?.Date, Bis?.Date),
            _ => (null, null),
        };
    }

    public bool Passt(Ticket t, DateTime heute)
    {
        var wert = Wert(t, Feld);
        if (Preset == DatumsPreset.KeinDatum) return wert is null;
        if (wert is null) return false;
        var (von, bis) = Bereich(heute);
        var tag = wert.Value.Date;
        return (von is null || tag >= von) && (bis is null || tag <= bis);
    }

    // ── Text für den Chip ───────────────────────────────────────
    public string Beschriftung()
    {
        if (Preset != DatumsPreset.Zeitraum) return $"{Anzeige(Feld)}: {Anzeige(Preset)}";
        var von = Von?.ToString("dd.MM.yy");
        var bis = Bis?.ToString("dd.MM.yy");
        return (von, bis) switch
        {
            (not null, not null) when von == bis => $"{Anzeige(Feld)}: {von}",
            (not null, not null) => $"{Anzeige(Feld)}: {von} – {bis}",
            (not null, null) => $"{Anzeige(Feld)}: ab {von}",
            (null, not null) => $"{Anzeige(Feld)}: bis {bis}",
            _ => Anzeige(Feld),
        };
    }

    // ── Adresse (Query-Parameter) lesen und schreiben ───────────
    public string AlsText()
    {
        if (Preset != DatumsPreset.Zeitraum) return Kurz[Preset];
        static string D(DateTime? d) => d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
        return $"{D(Von)}..{D(Bis)}";
    }

    public static DatumsFilter? Lesen(DatumsFeld feld, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        foreach (var (preset, kurz) in Kurz)
            if (string.Equals(kurz, text, StringComparison.OrdinalIgnoreCase)) return new DatumsFilter(feld, preset);

        var teile = text.Split("..");
        if (teile.Length != 2) return null;
        var von = Datum(teile[0]);
        var bis = Datum(teile[1]);
        if (von is null && bis is null) return null;
        if (von > bis) (von, bis) = (bis, von);
        return new DatumsFilter(feld, DatumsPreset.Zeitraum, von, bis);

        static DateTime? Datum(string s) =>
            DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }
}
