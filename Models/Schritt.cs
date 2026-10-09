using System.ComponentModel.DataAnnotations;

namespace Ticketsystem.Models;

/// <summary>Eine datierte Notiz zu einem Ticket („an Tag X ist Folgendes passiert“).</summary>
public class Schritt
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public DateTime Datum { get; set; } = DateTime.Now;

    [Required, MaxLength(4000)]
    public string Text { get; set; } = "";
}
