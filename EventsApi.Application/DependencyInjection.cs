using EventsApi.Application.Interfaces;
using EventsApi.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventsApi.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Метод добавления сервис>jd
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        return services;
    }
}