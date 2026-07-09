using EventFlow.Events.Application.Interfaces;
using EventFlow.Events.Infrastructure.Context;
using EventFlow.Events.Infrastructure.Repositories;
using EventFlow.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlow.Events.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Метод добавления инфраструктурных сервисов
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.Configure<JwtTokenSettings>(configuration.GetSection(nameof(JwtTokenSettings)));

        // добавляем репозитории
        services.AddScoped<IEventRepository, DbEventRepository>();

       

        return services;
    }
}