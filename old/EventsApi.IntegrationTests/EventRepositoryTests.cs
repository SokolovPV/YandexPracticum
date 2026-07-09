using EventsApi.Domain.Entities;
using EventsApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EventsApi.IntegrationTests
{
	public class EventRepositoryTests : IClassFixture<PostgreSqlFixture>
	{
		private readonly PostgreSqlFixture postgreSqlFixture;
		public EventRepositoryTests(PostgreSqlFixture _postgreSqlFixture) => postgreSqlFixture = _postgreSqlFixture;

		[Fact]
		public async Task AddAsync_SaveEventToDatabase()
		{
			// Arrange
			await postgreSqlFixture.ResetDatabaseAsync();
			using var context = await postgreSqlFixture.CreateContextAsync();

			var repository = new DbEventRepository(context);
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);

			// Act
			await repository.AddAsync(@event, CancellationToken.None);

			// Assert — читаем из реальной БД через отдельный контекст
			using var verifyContext = await postgreSqlFixture.CreateContextAsync();
			var saved = await verifyContext.Events
					.FirstOrDefaultAsync(b => b.Id == @event.Id, CancellationToken.None);

			Assert.NotNull(saved);
			Assert.Equal(@event.Title, saved.Title);
		}

		[Fact]
		public async Task GetByIdAsync_GetEventFromDatabase()
		{
			// Arrange
			await postgreSqlFixture.ResetDatabaseAsync();
			using var context = await postgreSqlFixture.CreateContextAsync();
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);
			await context.Events.AddAsync(@event, CancellationToken.None);
			await context.SaveChangesAsync(CancellationToken.None);



			// Act
			using var verifyContext = await postgreSqlFixture.CreateContextAsync();
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
			await postgreSqlFixture.ResetDatabaseAsync();
			using var context = await postgreSqlFixture.CreateContextAsync();
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);
			await context.Events.AddAsync(@event, CancellationToken.None);
			await context.SaveChangesAsync(CancellationToken.None);
			var repository = new DbEventRepository(context);
			var updateTitle = "Update Title";
			@event.Title = updateTitle;
			await repository.UpdateAsync(@event, CancellationToken.None);


			// Act
			using var verifyContext = await postgreSqlFixture.CreateContextAsync();
			var result = await verifyContext.Events.FirstOrDefaultAsync(q => q.Id == @event.Id, CancellationToken.None);

			// Assert

			Assert.NotNull(result);
			Assert.Equal(updateTitle, result.Title);
		}

		[Fact]
		public async Task DeleteAsync_ReturnsTrue()
		{
			// Arrange
			await postgreSqlFixture.ResetDatabaseAsync();
			using var context = await postgreSqlFixture.CreateContextAsync();
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);
			await context.AddAsync(@event, CancellationToken.None);
			await context.SaveChangesAsync(CancellationToken.None);
			using var verifyContext = await postgreSqlFixture.CreateContextAsync();
			var repository = new DbEventRepository(verifyContext);
			//Act
			var result = await repository.DeleteAsync(@event.Id, CancellationToken.None);
			//Assert
			Assert.True(result);
		}

		[Fact]
		public async Task CountAsync_WithFilter_ReturnsEvents()
		{
			// Arrange
			await postgreSqlFixture.ResetDatabaseAsync();
			using var context = await postgreSqlFixture.CreateContextAsync();
			for (var i = 1; i <= 10; i++)
			{
				await context.Events.AddAsync(Event.Create($"Test Event #{i}", DateTime.UtcNow, DateTime.UtcNow.AddDays(i), i), CancellationToken.None);
			}
			await context.SaveChangesAsync(CancellationToken.None);

			// Act
			using var verifyContext = await postgreSqlFixture.CreateContextAsync();
			var repository = new DbEventRepository(verifyContext);
			var result = await repository.CountAsync(q => q.EndAt > DateTime.UtcNow.AddDays(2), CancellationToken.None);

			//Assert
			Assert.Equal(8, result);
		}

		[Fact]
		public async Task ListAsync_WithFilterByDate_ReturnsEvents()
		{
			// Arrange
			await postgreSqlFixture.ResetDatabaseAsync();
			using var context = await postgreSqlFixture.CreateContextAsync();
			for (var i = 1; i <= 10; i++)
			{
				await context.Events.AddAsync(Event.Create($"Test Event #{i}", DateTime.UtcNow, DateTime.UtcNow.AddDays(i), i), CancellationToken.None);
			}
			await context.SaveChangesAsync(CancellationToken.None);

			// Act
			using var verifyContext = await postgreSqlFixture.CreateContextAsync();
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
			await postgreSqlFixture.ResetDatabaseAsync();
			using var context = await postgreSqlFixture.CreateContextAsync();
			await context.Events.AddAsync(Event.Create($"Вечер в кино", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5), CancellationToken.None);
			await context.Events.AddAsync(Event.Create($"Ужин в ресторане", DateTime.UtcNow, DateTime.UtcNow.AddDays(2), 5), CancellationToken.None);
			await context.Events.AddAsync(Event.Create($"Праздник", DateTime.UtcNow, DateTime.UtcNow.AddDays(3), 5), CancellationToken.None);
			await context.Events.AddAsync(Event.Create($"ВеЧерИноЧка ", DateTime.UtcNow, DateTime.UtcNow.AddDays(4), 5), CancellationToken.None);
			await context.SaveChangesAsync(CancellationToken.None);

			// Act
			using var verifyContext = await postgreSqlFixture.CreateContextAsync();
			var repository = new DbEventRepository(verifyContext);
			var result = await repository.ListAsync(q => EF.Functions.ILike(q.Title, $"%Вечер%"), page: 1, pageSize: 3, CancellationToken.None);

			//Assert
			Assert.NotEmpty(result);
			Assert.Equal(2, result.Count);
		}

	}
}