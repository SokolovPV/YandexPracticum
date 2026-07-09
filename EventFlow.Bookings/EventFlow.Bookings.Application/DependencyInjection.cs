using EventFlow.Bookings.Application.Interfaces;
using EventFlow.Bookings.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlow.Bookings.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
      // добавляем службы
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}