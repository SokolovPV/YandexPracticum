using EventFlow.Bookings.Infrastructure.Context;
using EventFlow.Bookings.Presentation.ExceptionFilter;
using EventFlow.Bookings.Application;
using EventFlow.Bookings.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using Serilog.Formatting.Compact;

namespace EventFlow.Bookings.Presentation
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);
			builder.Host.UseSerilog((ctx, cfg) =>
				cfg.ReadFrom.Configuration(ctx.Configuration)
				.WriteTo.Console(new CompactJsonFormatter()));

			builder.Services.AddInfrastructureServices(builder.Configuration);
			builder.Services.AddApplicationServices(builder.Configuration);
			builder.Services.AddPresentationServices(builder.Configuration);

			var app = builder.Build();

			using (var scope = app.Services.CreateScope())
			{
				var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
				var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
				try
				{
					// Проверяем подключение к БД при старте
					await db.Database.CanConnectAsync();
					logger.LogInformation("Подключение к базе данных успешно установлено");
					await db.Database.MigrateAsync();
				}
				catch (NpgsqlException ex)
				{
					logger.LogCritical(ex, "Не удалось подключиться к PostgreSQL. Код ошибки: {SqlState}", ex.SqlState);
					throw;
				}
				catch (Exception ex)
				{
					logger.LogCritical(ex, "Критическая ошибка при подключении к базе данных");
					throw;
				}
			}
			
			app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
			
			if (app.Environment.IsDevelopment())
			{
				builder.Host.UseDefaultServiceProvider(options =>
				{
					options.ValidateOnBuild = true;
					options.ValidateScopes = true;
				});
				app.UseSwagger();
				app.UseSwaggerUI();
			}
  			
			app.UseOpenTelemetryPrometheusScrapingEndpoint();

			app.UseAuthentication();
			app.UseAuthorization();
			
			app.MapControllers();

			app.Run();
		}
	}
}