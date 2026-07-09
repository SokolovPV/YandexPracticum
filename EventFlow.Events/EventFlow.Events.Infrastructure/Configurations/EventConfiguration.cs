using EventFlow.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace EventFlow.Events.Infrastructure.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        builder.HasKey(b => b.Id);
        builder.HasIndex(u => u.Id).IsUnique();
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
            .IsRequired();
        builder.Property(q => q.EndAt)
            .HasColumnName("end_at")
            .IsRequired();
        builder.Property(q => q.TotalSeats)
            .HasColumnName("total_seats")
            .IsRequired();
        builder.Property(q => q.AvailableSeats)
            .HasColumnName("available_seats")
            .IsRequired();
    }
}