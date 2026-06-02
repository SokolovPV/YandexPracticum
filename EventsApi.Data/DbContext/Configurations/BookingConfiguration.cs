using EventsApi.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventsApi.DataAccess;

public class BookConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasIndex(u => u.Id).IsUnique();
        builder.HasKey(b => b.Id);
        builder.Property(q => q.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(q => q.EventId)
           .IsRequired();
        builder.Property(q => q.Status)
           .HasColumnName("status")
           .IsRequired();
        builder.Property(q => q.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(q => q.ProcessedAt)
            .HasColumnName("processed_at");

        builder.HasOne(q => q.Event)
            .WithMany(q => q.Bookings)
            .HasForeignKey(q => q.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}