using System.ComponentModel.DataAnnotations;

namespace Ticketsystem.Models;

/// <summary>Ein Bild (z. B. Screenshot) in der Beschreibung oder einem Schritt. Im Text steht es als ![Name](bilder/ID).</summary>
public class Bild
{
    public int Id { get; set; }
    public int TicketId { get; set; }

    [MaxLength(260)] public string Dateiname { get; set; } = "";
    [MaxLength(100)] public string ContentType { get; set; } = "image/png";
    public byte[] Daten { get; set; } = [];
    public long Groesse { get; set; }
    public DateTime ErstelltAm { get; set; } = DateTime.Now;
}
