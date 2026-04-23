using EventsApi.Application.CustomException;
using EventsApi.Infrastructure.Interfaces;
using EventsApi.Models.Domain;

namespace EventsApi.Application.Services;

/// <summary>
/// Фоновый сервис для регистрации бронирования
/// </summary>
/// <param name="scopeFactory"></param>
/// <param name="logger"></param>
public class BookingBackgroundService(IServiceScopeFactory scopeFactory, ILogger<BookingBackgroundService> logger) : BackgroundService
{
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
    private readonly int ProcessingDelay = 2;
    private readonly int PollingInterval = 2;
    private const string key_not_found_exception = "Идентификатор мероприятия не найден.";
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Фоновая служба {backgroundServiceName} запущена", nameof(BookingBackgroundService));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                //var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                Func<Booking, bool> query = e => e.Status == BookingStatus.Pending;
                var pendingBookings = await bookingRepository.ListAsync(query, stoppingToken);
                var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
                await Task.WhenAll(tasks);



                // foreach (var booking in bookings)
                // {

                //     try
                //     {
                //         booking.Status = BookingStatus.Confirmed;
                //         var @event = await eventRepository.GetByIdAsync(booking.EventId, stoppingToken);
                //         if (@event is null)
                //             throw new NullReferenceException($"Мероприятие {booking.EventId} удалено, создание бронирования невозможно.");
                //     }
                //     catch (NullReferenceException ex)
                //     {
                //         logger.LogWarning(ex.Message);
                //         booking.Status = BookingStatus.Rejected;
                //     }
                //     finally
                //     {
                //         booking.ProcessedAt = DateTime.UtcNow;
                //         await bookingRepository.UpdateAsync(booking, stoppingToken);
                //     }
                //     logger.LogDebug("Обработка бронирования {currentBooking} завершена", booking.Id);
                // }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Сервис обработки бронирования завершен по требованию.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обработке бронирования");
            }
            await Task.Delay(TimeSpan.FromSeconds(PollingInterval), stoppingToken);
        }
    }
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Фоновая служба {backgroundServiceName} остановлена", nameof(BookingBackgroundService));
        return base.StopAsync(cancellationToken);
    }

    public async Task ProcessBookingAsync(Booking booking, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        Event? _event = await eventRepository.GetByIdAsync(booking.EventId, cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(ProcessingDelay), cancellationToken);


            if (_event is null)
            {
                logger.LogWarning("Идентификатор мероприятия {Id} не найден.", booking.EventId);
                booking.Reject();
                throw new KeyNotExistException();
            }

            try
            {
                await _processingSemaphore.WaitAsync(cancellationToken);

                booking.Confirm();
                await eventRepository.UpdateAsync(_event, cancellationToken);
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            booking.Reject();
            if (_event != null)
            {
                _event.ReleaseSeats();
                await eventRepository.UpdateAsync(_event, cancellationToken);
                logger.LogInformation("Свободные места для события {Id} - восстановлены.", _event.Id);
            }

            if (ex is not OperationCanceledException)
                logger.LogError(ex, "Ошибка при обработке бронирования {ID}", booking.Id);

            throw;
        }
        finally
        {
            await bookingRepository.UpdateAsync(booking, cancellationToken);
        }
    }
}