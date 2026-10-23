using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Makanak.Domain.Entities;

namespace Makanak.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<NegotiationHistory> NegotiationHistories => Set<NegotiationHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── ApplicationUser ───────────────────────────────────────────────────────
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            entity.Property(u => u.UserType).IsRequired();
        });

        // ── Property ──────────────────────────────────────────────────────────────
        builder.Entity<Property>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Title).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(2000);
            entity.Property(p => p.Price).HasPrecision(18, 2).IsRequired();
            entity.Property(p => p.Status).IsRequired();

            // MVP: standard string location fields (replaces NetTopologySuite Point)
            entity.Property(p => p.City).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Address).HasMaxLength(500).IsRequired();

            // NFR-03: RowVersion for optimistic concurrency control
            entity.Property(p => p.RowVersion).IsRowVersion();

            entity.HasOne(p => p.Owner)
                  .WithMany(u => u.OwnedProperties)
                  .HasForeignKey(p => p.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PropertyImage (FR-04) ─────────────────────────────────────────────────
        builder.Entity<PropertyImage>(entity =>
        {
            entity.HasKey(pi => pi.Id);
            entity.Property(pi => pi.ImageUrl).HasMaxLength(2048).IsRequired();
            entity.Property(pi => pi.IsPrimary).IsRequired();

            entity.HasOne(pi => pi.Property)
                  .WithMany(p => p.Images)
                  .HasForeignKey(pi => pi.PropertyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Offer ─────────────────────────────────────────────────────────────────
        builder.Entity<Offer>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.CurrentPrice).HasPrecision(18, 2).IsRequired();
            entity.Property(o => o.Status).IsRequired();
            entity.Property(o => o.RoundCount).IsRequired();

            // NFR-03: RowVersion for optimistic concurrency control
            entity.Property(o => o.RowVersion).IsRowVersion();

            entity.HasOne(o => o.Property)
                  .WithMany(p => p.Offers)
                  .HasForeignKey(o => o.PropertyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(o => o.Seeker)
                  .WithMany(u => u.SubmittedOffers)
                  .HasForeignKey(o => o.SeekerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── NegotiationHistory (FR-09) ────────────────────────────────────────────
        builder.Entity<NegotiationHistory>(entity =>
        {
            entity.HasKey(nh => nh.Id);
            entity.Property(nh => nh.ActionType).IsRequired();
            entity.Property(nh => nh.ProposedPrice).HasPrecision(18, 2).IsRequired();
            entity.Property(nh => nh.Timestamp).IsRequired();

            // Immutable audit log — no cascade delete from Offer
            entity.HasOne(nh => nh.Offer)
                  .WithMany()
                  .HasForeignKey(nh => nh.OfferId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Actor reference — no cascade delete from ApplicationUser
            entity.HasOne(nh => nh.ActionByUser)
                  .WithMany()
                  .HasForeignKey(nh => nh.ActionByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
