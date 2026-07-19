using EventFlow.Events.Application;
using EventFlow.Events.Infrastructure;
using EventFlow.Events.Infrastructure.Context;
using EventFlow.Events.Presentation.ExceptionFilter;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Events.Presentation
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);
			// Логирование в консоль
			builder.Logging.AddConsole();

			builder.Services.AddInfrastructureServices(builder.Configuration);
			builder.Services.AddApplicationServices(builder.Configuration);
			builder.Services.AddPresentationServices(builder.Configuration);

			var app = builder.Build();
			using (var scope = app.Services.CreateScope())
			{
				var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
				await db.Database.MigrateAsync();
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


			app.UseAuthentication();
			app.UseAuthorization();

			app.MapControllers();
			app.Run();
		}
	}
}