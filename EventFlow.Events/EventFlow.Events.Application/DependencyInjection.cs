using EventFlow.Events.Application.Interfaces;
using EventFlow.Events.Application.Options;
using EventFlow.Events.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlow.Events.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
      // добавляем службы
        services.AddScoped<IEventService, EventService>();
         services.Configure<RedisOptions>(configuration.GetSection(nameof(RedisOptions)));
        return services;
    }
}