using System.Reflection;
using EventsApi.ExceptionFilter;
using Microsoft.EntityFrameworkCore;

namespace EventsApi
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);
			// Логирование в консоль
			builder.Logging.AddConsole();


			builder.Services.AddInfrastructure(builder.Configuration);
			builder.Services.AddApplication();

			// добавляем сервисы
			builder.Services.AddScoped<IEventService, EventService>();
			builder.Services.AddScoped<IBookingService, BookingService>();


			builder.Services.AddControllers();
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen(options =>
			{
				var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
				var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
				options.IncludeXmlComments(xmlPath);
			});

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

			app.MapControllers();

			app.Run();
		}
	}
}
