
using EventFlow.Bookings.Application.Interfaces;
using EventFlow.Bookings.Domain.Entities;
using EventFlow.Bookings.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventFlow.Bookings.Infrastructure.Services;

/// <summary>
/// Фоновый сервис для регистрации бронирования
/// </summary>
public class BookingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingBackgroundService> _logger;
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
                await StartProcessBookingAsync(stoppingToken);
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
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        await bookingService.ProcessBookingAsync(booking, cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Фоновая служба {ServiceName} останавливается...", nameof(BookingBackgroundService));
        await base.StopAsync(cancellationToken);
    }


    public async Task StartProcessBookingAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

        var pendingBookings = await bookingRepository.ListAsync(
            e => e.Status == BookingStatus.Pending,
            stoppingToken);

        if (pendingBookings.Any())
        {
            _logger.LogInformation("Найдено {Count} бронирований для обработки", pendingBookings.Count());
        }

        // Запускаем параллельную обработку всех ожидающих бронирований
        var tasks = pendingBookings.Select(booking =>
            ProcessBookingAsync(booking, stoppingToken));

        await Task.WhenAll(tasks);
    }
}