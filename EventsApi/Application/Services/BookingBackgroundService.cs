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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Фоновая служба {backgroundServiceName} запущена", nameof(BookingBackgroundService));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                Func<Booking, bool> query = e => e.Status == BookingStatus.Pending;
                var bookings = await bookingRepository.ListAsync(query, stoppingToken);
                foreach (var booking in bookings)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    booking.Status = BookingStatus.Confirmed;
                    booking.ProcessedAt = DateTime.UtcNow;
                    await bookingRepository.UpdateAsync(booking, stoppingToken);

                    logger.LogInformation("Обработка бронирования {currentBooking} завершена", booking.Id);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обработке бронирования");
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Фоновая служба {backgroundServiceName} остановлена", nameof(BookingBackgroundService));
        return base.StopAsync(cancellationToken);
    }
}