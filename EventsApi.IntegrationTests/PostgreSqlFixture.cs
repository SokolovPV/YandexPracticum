using EventsApi.DataAccess;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Testcontainers.PostgreSql;

namespace EventsApi.IntegrationTests
{
	public class PostgreSqlFixture : IAsyncLifetime
	{
		private readonly PostgreSqlContainer _postgresContainer;

		public string ConnectionString => _postgresContainer.GetConnectionString();
		public AppDbContext DbContext { get; private set; }

		public PostgreSqlFixture()
		{
			_postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
					.WithCleanUp(true)
					.Build();
		}

		public async ValueTask InitializeAsync()
		{
			await _postgresContainer.StartAsync();
		}

		public async ValueTask DisposeAsync()
		{
			await _postgresContainer.DisposeAsync();
		}

		public async Task<AppDbContext> CreateContextAsync()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
					.UseNpgsql(_postgresContainer.GetConnectionString())
					.Options;

			var context = new AppDbContext(options);
			await context.Database.MigrateAsync();
			return context;
		}
		public async Task ResetDatabaseAsync()
		{
			using var context = await CreateContextAsync();
			await context.Database.ExecuteSqlRawAsync(
					"TRUNCATE TABLE bookings, events RESTART IDENTITY CASCADE");
		}
	}
}
