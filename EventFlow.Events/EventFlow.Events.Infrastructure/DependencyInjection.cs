using EventFlow.Events.Application.Interfaces;
using EventFlow.Events.Infrastructure.Context;
using EventFlow.Events.Infrastructure.Options;
using EventFlow.Events.Infrastructure.Repositories;
using EventFlow.Events.Infrastructure.Services;
using EventFlow.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace EventFlow.Events.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Метод добавления инфраструктурных сервисов
    /// </summary>
    public static async Task<IServiceCollection> AddInfrastructureServicesAsync(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' not found.");
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<KafkaOptions>(configuration.GetSection(nameof(KafkaOptions)));
               
        services.AddScoped<IEventRepository, DbEventRepository>();
        services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();
        services.AddSingleton<ICacheService, RedisCacheService>();

        var redisOptions = new ConfigurationOptions
        {
            EndPoints = { redisConnectionString },
            AbortOnConnectFail = false,
            ConnectRetry = 3,
            ConnectTimeout = 5000, // Тайм-аут подключения, мс
            SyncTimeout = 3000,      // Тайм-аут синхронных операций, мс
        };
        services.AddSingleton<IConnectionMultiplexer>(
            await ConnectionMultiplexer.ConnectAsync(redisOptions)
        ); 

        services.AddHostedService<KafkaTopicInitializer>();
        services.AddHostedService<BookingConfirmedConsumer>();
        return services;
    }
}