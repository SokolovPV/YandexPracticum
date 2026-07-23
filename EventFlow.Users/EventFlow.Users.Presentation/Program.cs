using EventFlow.Users.Infrastructure.Context;
using EventFlow.Users.Application;
using EventFlow.Users.Infrastructure;
using Microsoft.EntityFrameworkCore;
using EventFlow.Users.Presentation.ExceptionFilter;
using Serilog;
using Serilog.Formatting.Compact;

namespace EventFlow.Users.Presentation
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
			builder.Services.AddApplicationServices();
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

			app.MapPrometheusScrapingEndpoint(); // доступен по /metrics 
			app.MapControllers();

			app.Run();
		}
	}
}