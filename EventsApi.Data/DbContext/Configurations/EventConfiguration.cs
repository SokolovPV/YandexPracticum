using EventsApi.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace EventsApi.DataAccess;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        builder.HasKey(b => b.Id);
        builder.Property(q => q.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(e => e.Title)
            .HasColumnName("title")
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);
        builder.Property(q => q.StartAt)
            .HasColumnName("start_at")
            .HasColumnType("timestamptz")
            .IsRequired();
        builder.Property(q => q.EndAt)
            .HasColumnName("end_at")
            .HasColumnType("timestamptz")
            .IsRequired();
        builder.Property(q => q.TotalSeats)
            .HasColumnName("total_seats")
            .IsRequired();
        builder.Property(q => q.AvailableSeats)
            .HasColumnName("available_seats")
            .IsRequired();
        
        builder.HasMany(q=>q.Bookings)
        .WithOne(q=>q.Event)
        .HasForeignKey(q=>q.EventId)
        .OnDelete(DeleteBehavior.Cascade);
    }
}