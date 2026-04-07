
using EventsApi.Application.Interfaces;
using EventsApi.Application.Services;
using System.Reflection;

namespace EventsApi
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);
			if (builder.Environment.IsDevelopment())
			{
				builder.Host.UseDefaultServiceProvider(options =>
				{
					options.ValidateOnBuild = true;
					options.ValidateScopes = true;
				});
			}

			builder.Services.AddControllers();
			builder.Services.AddSwaggerGen(options =>
			{
				var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
				var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
				options.IncludeXmlComments(xmlPath);
			});
			builder.Services.AddScoped<IEventService, EventService>();

			var app = builder.Build();
			app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}
			app.MapControllers();
			app.Run();
		}
	}
}
