
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventFlow.Bookings.Infrastructure.Services;

/// <summary>
/// Фоновый сервис для регистрации бронирования
/// </summary>
public class BookingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingBackgroundService> _logger;
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
    private readonly int _processingDelay = 2; // Имитация задержки внешнего вызова
    private readonly int _pollingInterval = 2; // Интервал опроса новых бронирований

    public BookingBackgroundService(IServiceScopeFactory scopeFactory, ILogger<BookingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Фоновая служба {ServiceName} запущена", nameof(BookingBackgroundService));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                var pendingBookings = await bookingRepository.ListAsync(
                    e => e.Status == BookingStatus.Pending, 
                    stoppingToken);

                if (pendingBookings.Any())
                {
                    _logger.LogInformation("Найдено {Count} бронирований для обработки", pendingBookings.Count());
                }

                // Запускаем параллельную обработку всех ожидающих бронирований
                var tasks = pendingBookings.Select(booking => 
                    ProcessBookingAsync(booking, bookingRepository, eventRepository, stoppingToken));
                
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Сервис обработки бронирования завершен по требованию.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка в основном цикле обработки бронирований");
            }

            await Task.Delay(TimeSpan.FromSeconds(_pollingInterval), stoppingToken);
        }

        _logger.LogInformation("Фоновая служба {ServiceName} остановлена", nameof(BookingBackgroundService));
    }

    private async Task ProcessBookingAsync(
        Booking booking, 
        IBookingRepository bookingRepository, 
        IEventRepository eventRepository, 
        CancellationToken cancellationToken)
    {
        Event? @event = null;
        var bookingId = booking.Id;
        var eventId = booking.EventId;

        try
        {
            // 1. Имитация внешнего вызова ДО захвата семафора
            _logger.LogDebug("Имитация внешнего вызова для бронирования {BookingId}", bookingId);
            await Task.Delay(TimeSpan.FromSeconds(_processingDelay), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 2. Захватываем семафор перед критической секцией
            await _processingSemaphore.WaitAsync(cancellationToken);
            
            try
            {
                // 3. Проверяем существование события
                @event = await eventRepository.GetByIdAsync(eventId, cancellationToken);
                
                if (@event is null)
                {
                    // Событие было удалено — отклоняем бронирование
                    _logger.LogWarning(
                        "Событие с Id {EventId} не найдено. Бронирование {BookingId} отклоняется", 
                        eventId, bookingId);
                    
                    booking.Reject();
                    await bookingRepository.UpdateAsync(booking, cancellationToken);
                    return; // Завершаем обработку
                }

                // 4. Событие существует — подтверждаем бронирование
                _logger.LogInformation("Подтверждение бронирования {BookingId} для события {EventId}", 
                    bookingId, eventId);
                
                booking.Confirm();
                await bookingRepository.UpdateAsync(booking, cancellationToken);
                
                _logger.LogInformation("Бронирование {BookingId} успешно подтверждено", bookingId);
            }
            finally
            {
                // 5. Освобождаем семафор в любом случае
                _processingSemaphore.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Обработка бронирования {BookingId} прервана по требованию", bookingId);
            
            // Если отмена произошла во время удержания семафора, убеждаемся что он освобожден
            if (_processingSemaphore.CurrentCount == 0)
            {
                _processingSemaphore.Release();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Непредвиденная ошибка при обработке бронирования {BookingId}", bookingId);

            try
            {
                // 6. Отклоняем бронирование
                if (booking.Status != BookingStatus.Rejected)
                {
                    booking.Reject();
                    await bookingRepository.UpdateAsync(booking, cancellationToken);
                    _logger.LogInformation("Бронирование {BookingId} отклонено из-за ошибки", bookingId);
                }

                // 7. Возвращаем место в пул, если событие существует
                if (@event != null)
                {
                    @event.ReleaseSeats();
                    await eventRepository.UpdateAsync(@event, cancellationToken);
                    _logger.LogInformation(
                        "Место возвращено в пул для события {EventId} при обработке бронирования {BookingId}", 
                        eventId, bookingId);
                }
            }
            catch (Exception rollbackEx)
            {
                // Критическая ошибка при откате
                _logger.LogCritical(rollbackEx, 
                    "КРИТИЧЕСКИЙ СБОЙ при откате бронирования {BookingId} для события {EventId}",
                    bookingId, eventId);
            }

            // Убеждаемся, что семафор освобожден
            if (_processingSemaphore.CurrentCount == 0)
            {
                _processingSemaphore.Release();
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Фоновая служба {ServiceName} останавливается...", nameof(BookingBackgroundService));
        await base.StopAsync(cancellationToken);
    }
}