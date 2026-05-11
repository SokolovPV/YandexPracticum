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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Фоновая служба {backgroundServiceName} запущена", nameof(BookingBackgroundService));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                var pendingBookings = await bookingRepository.ListAsync(e => e.Status == BookingStatus.Pending, stoppingToken);
                var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
                await Task.WhenAll(tasks);
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
        Event? _event = null;


        await Task.Delay(TimeSpan.FromSeconds(ProcessingDelay), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _processingSemaphore.WaitAsync(cancellationToken);
            _event = await eventRepository.GetByIdAsync(booking.EventId, cancellationToken);

            if (_event is null)
            {
                logger.LogWarning("Идентификатор мероприятия {Id} не найден.", booking.EventId);
                throw new KeyNotExistException(booking.EventId, ConstantValues.key_not_found_exception);
            }

            booking.Confirm();
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

            throw;
        }
        finally
        {
            // вынес в блок finally что-бы всегда сохранялось бронировании из try catch
            await bookingRepository.UpdateAsync(booking, cancellationToken);
            _processingSemaphore.Release();
        }
    }
}