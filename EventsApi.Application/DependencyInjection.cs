using EventsApi.Application.Interfaces;
using EventsApi.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventsApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
         // добавляем службы
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        // добавляем фоновую службу бронирования
        services.AddHostedService<BookingBackgroundService>();
        return services;
    }
}