using System.ComponentModel.DataAnnotations;

namespace Ticketsystem.Models;

public class Ticket
{
    public int Id { get; set; }

    // ── Kopf ────────────────────────────────────────────────
    [Required(ErrorMessage = "Ticketnummer ist erforderlich.")]
    [MaxLength(50)]
    public string Ticketnummer { get; set; } = "";

    [Required(ErrorMessage = "Betreff ist erforderlich.")]
    [MaxLength(300)]
    public string Betreff { get; set; } = "";

    // ── Allgemeine Informationen ───────────────────────────
    // Können mehrere Werte enthalten, getrennt mit „; “ (siehe Auswahl.Trenner)
    [MaxLength(1000)] public string? Ansprechpartner { get; set; }
    [MaxLength(1000)] public string? Mandant { get; set; }
    [MaxLength(1000)] public string? Umfeld { get; set; }
    [MaxLength(300)] public string? FeatureBranch { get; set; }

    // ── Zeitangaben ─────────────────────────────────────────
    public DateTime? AufgabeErhalten { get; set; }
    public DateTime? Erinnerung { get; set; }
    public DateTime? Faellig { get; set; }
    public DateTime? ErledigtAm { get; set; }

    // ── Status ──────────────────────────────────────────────
    public TicketStatus Status { get; set; } = TicketStatus.Neu;
    [MaxLength(200)] public string? WartetAuf { get; set; }
    [MaxLength(1000)] public string? Wartegrund { get; set; }
    public DateTime? WartetSeit { get; set; }

    // ── Tests ───────────────────────────────────────────────
    public bool LokalTest { get; set; }
    public bool ConsultantTest { get; set; }

    // ── PR-Verwaltung ───────────────────────────────────────
    public bool PrGestellt { get; set; }
    public bool KeineTaskMehr { get; set; }

    // ── Erledigt ────────────────────────────────────────────
    public bool Erledigt { get; set; }

    // ── Beschreibung ────────────────────────────────────────
    public string? Beschreibung { get; set; }

    // ── Schritte (datierte Notizen) ─────────────────────────
    public List<Schritt> Schritte { get; set; } = new();

    // ── Tasks (wie in Azure DevOps) ─────────────────────────
    public List<Aufgabe> Aufgaben { get; set; } = new();

    public DateTime ErstelltAm { get; set; } = DateTime.Now;
    public DateTime GeaendertAm { get; set; } = DateTime.Now;

    // ── Abgeleitete Werte (nicht in der DB) ─────────────────
    public bool IstUeberfaellig => !Erledigt && Faellig is { } f && f < DateTime.Now;
    public bool ErinnerungFaellig => !Erledigt && Erinnerung is { } e && e <= DateTime.Now;

    /// <summary>Wie viele der vier Checkpunkte (Tests + PR) abgehakt sind.</summary>
    public int AufgabenGesamt => Aufgaben.Count;
    public int AufgabenErledigt => Aufgaben.Count(a => a.Status == AufgabeStatus.Erledigt);
    public double RestaufwandSumme => Aufgaben.Where(a => a.Status != AufgabeStatus.Erledigt).Sum(a => a.RestaufwandStunden ?? 0);

    public int Fortschritt => (LokalTest ? 1 : 0) + (ConsultantTest ? 1 : 0) + (PrGestellt ? 1 : 0) + (KeineTaskMehr ? 1 : 0);

    public Ticket Clone()
    {
        var c = (Ticket)MemberwiseClone();
        c.Schritte = Schritte.Select(s => new Schritt { Id = s.Id, TicketId = s.TicketId, Datum = s.Datum, Text = s.Text }).ToList();
        c.Aufgaben = Aufgaben.Select(a => new Aufgabe
        {
            Id = a.Id, TicketId = a.TicketId, Titel = a.Titel, Status = a.Status, RestaufwandStunden = a.RestaufwandStunden,
            Reihenfolge = a.Reihenfolge, ErstelltAm = a.ErstelltAm, ErledigtAm = a.ErledigtAm,
        }).ToList();
        return c;
    }
}
