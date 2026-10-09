using System.ComponentModel.DataAnnotations;

namespace Ticketsystem.Models;

/// <summary>Ein Task zu einem Ticket – wie die Tasks unter einem Work Item in Azure DevOps.</summary>
public class Aufgabe
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    [Required, MaxLength(300)]
    public string Titel { get; set; } = "";

    public AufgabeStatus Status { get; set; } = AufgabeStatus.Offen;

    /// <summary>Verbleibender Aufwand in Stunden (optional, wie „Remaining Work“ in Azure).</summary>
    public double? RestaufwandStunden { get; set; }

    /// <summary>Position innerhalb des Tickets (für die Sortierung).</summary>
    public int Reihenfolge { get; set; }

    public DateTime ErstelltAm { get; set; } = DateTime.Now;
    public DateTime? ErledigtAm { get; set; }
}

public enum AufgabeStatus
{
    Offen = 0,
    InArbeit = 1,
    Erledigt = 2,
}

public static class AufgabeStatusExtensions
{
    public static string Anzeige(this AufgabeStatus s) => s switch
    {
        AufgabeStatus.Offen => "Zu erledigen",
        AufgabeStatus.InArbeit => "In Arbeit",
        AufgabeStatus.Erledigt => "Erledigt",
        _ => s.ToString(),
    };

    public static string Symbol(this AufgabeStatus s) => s switch
    {
        AufgabeStatus.Offen => "radio_button_unchecked",
        AufgabeStatus.InArbeit => "clock_loader_40",
        AufgabeStatus.Erledigt => "check_circle",
        _ => "radio_button_unchecked",
    };

    public static string Css(this AufgabeStatus s) => "task-" + s.ToString().ToLowerInvariant();

    /// <summary>Nächster Status beim Anklicken: Zu erledigen → In Arbeit → Erledigt → Zu erledigen.</summary>
    public static AufgabeStatus Naechster(this AufgabeStatus s) => s switch
    {
        AufgabeStatus.Offen => AufgabeStatus.InArbeit,
        AufgabeStatus.InArbeit => AufgabeStatus.Erledigt,
        _ => AufgabeStatus.Offen,
    };
}
