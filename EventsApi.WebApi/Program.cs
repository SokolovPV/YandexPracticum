using System.Reflection;
using EventsApi.DataAccess;
using EventsApi.ExceptionFilter;
using EventsApi.WebApi.Application.Interfaces;
using EventsApi.WebApi.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace EventsApi
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);
			// Логирование в консоль
			builder.Logging.AddConsole();

			//Пподключаем базу данных
			var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
				?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

			builder.Services.AddDbContext<AppDbContext>(options =>
				options.UseNpgsql(connectionString));

			// добавляем репозитории
			builder.Services.AddScoped<IEventRepository, DbEventRepository>();
			builder.Services.AddScoped<IBookingRepository, DbBookingRepository>();
			// добавляем сервисы
			builder.Services.AddScoped<IEventService, EventService>();
			builder.Services.AddScoped<IBookingService, BookingService>();
			// добавляем фоновую службу бронирования
			builder.Services.AddHostedService<BookingBackgroundService>();

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
				db.Database.EnsureCreated();
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
