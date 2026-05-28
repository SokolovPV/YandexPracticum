using EventsApi.DataAccess;
using EventsApi.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Moq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using Testcontainers.PostgreSql;

namespace EventsApi.Test.InregrationTests
{
	public class EventRepositoryTests : IAsyncLifetime
	{
		private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
			.WithImage("postgres:16-alpine")
			.Build();

		public async ValueTask InitializeAsync()
		{
			await _postgres.StartAsync();
		}

		public async ValueTask DisposeAsync()
		{
			await _postgres.DisposeAsync();
		}

		private AppDbContext CreateContext()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
					.UseNpgsql(_postgres.GetConnectionString())
					.Options;

			var context = new AppDbContext(options);
			context.Database.Migrate();
			return context;
		}
		private async Task ResetDatabaseAsync()
		{
			await using var context = CreateContext();
			await context.Database.ExecuteSqlRawAsync(
					"TRUNCATE TABLE bookings, events RESTART IDENTITY CASCADE");
		}

		[Fact]
		public async Task AddAsync_SaveEventToDatabase()
		{
			// Arrange
			await ResetDatabaseAsync();
			await using var context = CreateContext();

			var repository = new DbEventRepository(context);
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);

			// Act
			await repository.AddAsync(@event, CancellationToken.None);

			// Assert — читаем из реальной БД через отдельный контекст
			await using var verifyContext = CreateContext();
			var saved = await verifyContext.Events
					.FirstOrDefaultAsync(b => b.Id == @event.Id, CancellationToken.None);

			Assert.NotNull(saved);
			Assert.Equal(@event.Title, saved.Title);
		}

		[Fact]
		public async Task GetByIdAsync_GetEventFromDatabase()
		{
			// Arrange
			await ResetDatabaseAsync();
			await using var context = CreateContext();
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);
			await context.Events.AddAsync(@event, CancellationToken.None);
			await context.SaveChangesAsync(CancellationToken.None);



			// Act
			await using var verifyContext = CreateContext();
			var repository = new DbEventRepository(verifyContext);
			var result = await repository.GetByIdAsync(@event.Id, CancellationToken.None);

			//Assert
			Assert.NotNull(result);
			Assert.Equal(@event.Title, result.Title);
		}

		[Fact]
		public async Task UpdateAsync_UpdateEventInDatabase()
		{
			// Arrange
			await ResetDatabaseAsync();
			await using var context = CreateContext();
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);
			await context.Events.AddAsync(@event, CancellationToken.None);
			await context.SaveChangesAsync(CancellationToken.None);
			var repository = new DbEventRepository(context);
			var updateTitle = "Update Title";
			@event.Title = updateTitle;
			await repository.UpdateAsync(@event, CancellationToken.None);


			// Act
			await using var verifyContext = CreateContext();
			var result = await verifyContext.Events.FirstOrDefaultAsync(q => q.Id == @event.Id, CancellationToken.None);

			// Assert

			Assert.NotNull(result);
			Assert.Equal(updateTitle, result.Title);
		}

		[Fact]
		public async Task DeleteAsync_ReturnsTrue()
		{
			// Arrange
			await ResetDatabaseAsync();
			await using var context = CreateContext();
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);
			await context.AddAsync(@event, CancellationToken.None);
			await context.SaveChangesAsync(CancellationToken.None);
			// Assert
			await using var verifyContext = CreateContext();
			var repository = new DbEventRepository(verifyContext);
			var result = await repository.DeleteAsync(@event.Id, CancellationToken.None);

			Assert.True(result);
		}

		[Fact]
		public async Task CountAsync_WithFilter_ReturnsEvents()
		{
			// Arrange
			await ResetDatabaseAsync();
			await using var context = CreateContext();
			for (var i = 1; i <= 10; i++)
			{
				await context.Events.AddAsync(Event.Create($"Test Event #{i}", DateTime.UtcNow, DateTime.UtcNow.AddDays(i), i), CancellationToken.None);
			}
			await context.SaveChangesAsync(CancellationToken.None);

			// Act
			await using var verifyContext = CreateContext();
			var repository = new DbEventRepository(verifyContext);
			var result = await repository.CountAsync(q => q.EndAt > DateTime.UtcNow.AddDays(2), CancellationToken.None);

			//Assert
			Assert.Equal(8, result);
		}

		[Fact]
		public async Task ListAsync_WithFilterByDate_ReturnsEvents()
		{
			// Arrange
			await ResetDatabaseAsync();
			await using var context = CreateContext();
			for (var i = 1; i <= 10; i++)
			{
				await context.Events.AddAsync(Event.Create($"Test Event #{i}", DateTime.UtcNow, DateTime.UtcNow.AddDays(i), i), CancellationToken.None);
			}
			await context.SaveChangesAsync(CancellationToken.None);

			// Act
			await using var verifyContext = CreateContext();
			var repository = new DbEventRepository(verifyContext);
			var result = await repository.ListAsync(q => q.EndAt > DateTime.UtcNow.AddDays(2), page: 2, pageSize: 3, CancellationToken.None);

			//Assert
			Assert.NotEmpty(result);
			Assert.Equal(3, result.Count);
		}

		[Fact]
		public async Task ListAsync_WithFilterByTitle_ReturnsEvents()
		{
			// Arrange
			await ResetDatabaseAsync();
			await using var context = CreateContext();
			await context.Events.AddAsync(Event.Create($"Вечер в кино", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5), CancellationToken.None);
			await context.Events.AddAsync(Event.Create($"Ужин в ресторане", DateTime.UtcNow, DateTime.UtcNow.AddDays(2), 5), CancellationToken.None);
			await context.Events.AddAsync(Event.Create($"Праздник", DateTime.UtcNow, DateTime.UtcNow.AddDays(3), 5), CancellationToken.None);
			await context.Events.AddAsync(Event.Create($"ВеЧерИноЧка ", DateTime.UtcNow, DateTime.UtcNow.AddDays(4), 5), CancellationToken.None);
			await context.SaveChangesAsync(CancellationToken.None);

			// Act
			await using var verifyContext = CreateContext();
			var repository = new DbEventRepository(verifyContext);
			var result = await repository.ListAsync(q => EF.Functions.ILike(q.Title, $"%Вечер%"), page: 1, pageSize: 3, CancellationToken.None);

			//Assert
			Assert.NotEmpty(result);
			Assert.Equal(2, result.Count);
		}

	}
}