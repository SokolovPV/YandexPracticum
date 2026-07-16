using EventFlow.Users.Application.Interfaces;
using EventFlow.Users.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlow.Users.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // добавляем службы
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        return services;
    }
}