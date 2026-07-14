using EventFlow.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventFlow.Events.Infrastructure.Configurations;

/// <summary>
/// таблица обработанных сообщений.
/// </summary>
public class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired();
    }
}