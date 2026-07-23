using System.Linq.Expressions;
using EventFlow.Bookings.Application.Interfaces;
using EventFlow.Bookings.Domain.Entities;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EventFlow.Bookings.Infrastructure.Repositories;

/// <summary>
/// Декоратор для репозитория с обработкой ошибок PostgreSQL
/// </summary>
public class DatabaseErrorHandlingRepositoryDecorator : IBookingRepository
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<DatabaseErrorHandlingRepositoryDecorator> _logger;

    public DatabaseErrorHandlingRepositoryDecorator(
        IBookingRepository bookingrepository,
        ILogger<DatabaseErrorHandlingRepositoryDecorator> logger)
    {
        _bookingRepository = bookingrepository;
        _logger = logger;
    }

    public async Task AddAsync(Booking booking, CancellationToken ct)
    {
        await ExecuteWithErrorHandlingAsync(
            () => _bookingRepository.AddAsync(booking, ct),
            nameof(AddAsync),
            $"BookingId: {booking.Id}");
    }

    public async Task<bool> DeleteAsync(Guid bookingId, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _bookingRepository.DeleteAsync(bookingId, ct),
            nameof(DeleteAsync),
            $"BookingId: {bookingId}");
    }

    public async Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _bookingRepository.GetByIdAsync(bookingId, ct),
            nameof(GetByIdAsync),
            $"BookingId: {bookingId}");
    }

    public async Task<List<Booking>> ListAsync(Expression<Func<Booking, bool>>? query, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _bookingRepository.ListAsync(query, ct),
            nameof(ListAsync),
            $"HasQuery: {query != null}");
    }

    public async Task UpdateAsync(Booking booking, CancellationToken ct)
    {
        await ExecuteWithErrorHandlingAsync(
            () => _bookingRepository.UpdateAsync(booking, ct),
            nameof(UpdateAsync),
            $"BookingId: {booking.Id}");
    }

    public async Task<bool> ConfirmAsync(Guid bookingId, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _bookingRepository.ConfirmAsync(bookingId, ct),
            nameof(ConfirmAsync),
            $"BookingId: {bookingId}");
    }

    public async Task<bool> RejectAsync(Guid bookingId, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _bookingRepository.RejectAsync(bookingId, ct),
            nameof(RejectAsync),
            $"BookingId: {bookingId}");
    }

    #region Private Methods

    private async Task ExecuteWithErrorHandlingAsync(
        Func<Task> operation,
        string methodName,
        string context)
    {
        try
        {
            await operation();
        }
        catch (PostgresException ex)
        {
            LogPostgresError(ex, methodName, context);
            throw;
        }
        catch (NpgsqlException ex)
        {
            LogInfrastructureError(ex, methodName, context);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Неожиданная ошибка в методе {MethodName}. {Context}", 
                methodName, context);
            throw;
        }
    }

    private async Task<T> ExecuteWithErrorHandlingAsync<T>(
        Func<Task<T>> operation,
        string methodName,
        string context)
    {
        try
        {
            return await operation();
        }
        catch (PostgresException ex)
        {
            LogPostgresError(ex, methodName, context);
            throw;
        }
        catch (NpgsqlException ex)
        {
            LogInfrastructureError(ex, methodName, context);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Неожиданная ошибка в методе {MethodName}. {Context}", 
                methodName, context);
            throw;
        }
    }

    private void LogPostgresError(PostgresException ex, string methodName, string context)
    {
        _logger.LogError(ex, 
            "Ошибка PostgreSQL в методе {MethodName}. {Context}. Код: {SqlState}",
            methodName, context, ex.SqlState);
    }

    private void LogInfrastructureError(NpgsqlException ex, string methodName, string context)
    {
        _logger.LogError(ex, 
            "Инфраструктурная ошибка в методе {MethodName}. {Context}",
            methodName, context);
    }

    #endregion
}
