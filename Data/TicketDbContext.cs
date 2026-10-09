using Microsoft.EntityFrameworkCore;
using Ticketsystem.Models;

namespace Ticketsystem.Data;

public class TicketDbContext(DbContextOptions<TicketDbContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Schritt> Schritte => Set<Schritt>();
    public DbSet<Aufgabe> Aufgaben => Set<Aufgabe>();
    public DbSet<Bild> Bilder => Set<Bild>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Ticket>(e =>
        {
            e.HasIndex(t => t.Ticketnummer).IsUnique();
            e.HasIndex(t => t.Status);
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            e.Ignore(t => t.IstUeberfaellig);
            e.Ignore(t => t.ErinnerungFaellig);
            e.Ignore(t => t.Fortschritt);
            e.Ignore(t => t.AufgabenGesamt);
            e.Ignore(t => t.AufgabenErledigt);
            e.Ignore(t => t.RestaufwandSumme);
            e.HasMany(t => t.Schritte)
             .WithOne(s => s.Ticket)
             .HasForeignKey(s => s.TicketId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(t => t.Aufgaben)
             .WithOne(a => a.Ticket)
             .HasForeignKey(a => a.TicketId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Bild>(e =>
        {
            e.ToTable("Bilder");
            e.HasIndex(x => x.TicketId);
            e.HasOne<Ticket>().WithMany().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Aufgabe>(e =>
        {
            e.ToTable("Aufgaben");
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        });
    }
}
