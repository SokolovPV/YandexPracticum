using EventsApi.Application.Interfaces;
using EventsApi.Application.Services;
using EventsApi.Infrastructure.Context;
using EventsApi.Infrastructure.Repositories;
using EventsApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventsApi.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Метод добавления сервис>jd
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        //Пподключаем базу данных
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // добавляем репозитории
        services.AddScoped<IEventRepository, DbEventRepository>();
        services.AddScoped<IBookingRepository, DbBookingRepository>();
		    services.AddScoped<IUserRepository, UserRepository>();

		    // регистрируем фоновую службу
		    services.AddHostedService<BookingBackgroundService>();

		    services.AddScoped<IPasswordHasher, CustomPasswordHasher>();

		    return services;
    }
}