using EventFlow.Bookings.Application.Interfaces;
using EventFlow.Bookings.Infrastructure.Context;
using EventFlow.Bookings.Infrastructure.Options;
using EventFlow.Bookings.Infrastructure.Repositories;
using EventFlow.Bookings.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventFlow.Bookings.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Метод добавления инфраструктурных сервисов
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Временно создаем ServiceProvider для получения логгера
        using var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("InfrastructureSetup");


        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            logger.LogCritical(
                "Строка подключения 'DefaultConnection' не найдена в конфигурации. " +
                "Проверьте appsettings.json или переменные окружения."
            );
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        services.AddDbContext<AppDbContext>(options =>
         options.UseNpgsql(connectionString));

        var kafkaOptions = configuration.GetSection(nameof(KafkaOptions));
        if (kafkaOptions is null)
        {
            logger.LogCritical(
                "Настройки брокера сообщений не найдена в конфигурации. " +
                "Проверьте appsettings.json или переменные окружения."
            );
            throw new InvalidOperationException("КafkaOptions settings not found.");
        }
        services.Configure<KafkaOptions>(kafkaOptions);

        services.AddScoped<IBookingRepository, DbBookingRepository>();
        services.AddScoped<IBookingConfirmedProducer, BookingConfirmedProducer>();

        services.AddHostedService<BookingBackgroundService>();

        return services;
    }
}