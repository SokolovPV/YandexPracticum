using EventFlow.Events.Application.Interfaces;
using EventFlow.Events.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlow.Events.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
      // добавляем службы
        services.AddScoped<IEventService, EventService>();

        return services;
    }
}