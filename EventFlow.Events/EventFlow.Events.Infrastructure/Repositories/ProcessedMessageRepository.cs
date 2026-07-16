using EventFlow.Events.Application.Interfaces;
using EventFlow.Events.Domain.Entities;
using EventFlow.Events.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Events.Infrastructure.Repositories;

/// <summary>
/// Репозиторий хранения обработанных сообщений.
/// </summary>
public class ProcessedMessageRepository(AppDbContext appDbContext) : IProcessedMessageRepository
{
    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct)
    {
        return await appDbContext.ProcessedMessages.AnyAsync(x => x.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(Guid id, CancellationToken ct)
    {
        await appDbContext.ProcessedMessages.AddAsync(new ProcessedMessage
        {
            Id = id,
            ProcessedAt = DateTime.UtcNow
        }, ct);

        await appDbContext.SaveChangesAsync(ct);
    }
}
