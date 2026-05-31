using EventsApi.DataAccess;
using EventsApi.Models.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Testcontainers.PostgreSql;

namespace EventsApi.IntegrationTests
{
	public class BookingRepositoryTests : IAsyncLifetime
	{
		private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
		public async ValueTask DisposeAsync()
		{
			await _postgres.DisposeAsync();
		}

		public async ValueTask InitializeAsync()
		{
			await _postgres.StartAsync();
		}

		private async Task<AppDbContext> CreateContextAsync()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
					.UseNpgsql(_postgres.GetConnectionString())
					.Options;

			var context = new AppDbContext(options);
			await context.Database.MigrateAsync();
			return context;
		}
		private async Task ResetDatabaseAsync()
		{
			using var context = await CreateContextAsync();
			await context.Database.ExecuteSqlRawAsync(
					"TRUNCATE TABLE bookings, events RESTART IDENTITY CASCADE");
		}


		[Fact]
		public async Task AddAsync_SaveBookingToDatabase()
		{
			// Arrange
			await ResetDatabaseAsync();
			using var context = await CreateContextAsync();

			var bookingRepository = new DbBookingRepository(context);
			var eventRepository = new DbEventRepository(context);
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);
			await eventRepository.AddAsync(@event, CancellationToken.None);
			var booking = Booking.Create(@event.Id);

			//Act
			await bookingRepository.AddAsync(booking, CancellationToken.None);

			// Assert — читаем из реальной БД через отдельный контекст
			using var verifyContext = await CreateContextAsync();
			var saved = await verifyContext.Bookings
					.FirstOrDefaultAsync(b => b.Id == booking.Id, CancellationToken.None);

			Assert.NotNull(saved);
			Assert.Equal(booking.Id, saved.Id);
		}
		[Fact]
		public async Task AddAsync_WithNotExistEvent_ReturnsThrowDbUpdateException()
		{
			// Arrange
			await ResetDatabaseAsync();
			using var context = await CreateContextAsync();

			var bookingRepository = new DbBookingRepository(context);
			var booking = Booking.Create(Guid.NewGuid());


			// Act Assert
			await Assert.ThrowsAsync<DbUpdateException>(async () => await bookingRepository.AddAsync(booking, CancellationToken.None));
		}


		[Fact]
		public async Task GetByIdAsync_ExistingBooking_ShouldReturnBooking()
		{
			// Arrange
			await ResetDatabaseAsync();
			var ct = CancellationToken.None;
			using var context = await CreateContextAsync();
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);
			var booking = Booking.Create(@event.Id);
			await context.Events.AddAsync(@event, ct);
			await context.Bookings.AddAsync(booking, ct);
			await context.SaveChangesAsync(ct);

			// Act
			using var verifyContext = await CreateContextAsync();
			var bookingRepository = new DbBookingRepository(context);
			var result = await bookingRepository.GetByIdAsync(booking.Id, ct);

			// Assert
			Assert.NotNull(result);
			Assert.Equal(booking.Id, result.Id);
		}
		[Fact]
		public async Task GetByIdAsync_NonExistingBooking_ShouldReturnNull()
		{
			// Arrange
			var nonExistingId = Guid.NewGuid();

			// Act
			using var verifyContext = await CreateContextAsync();
			var bookingRepository = new DbBookingRepository(verifyContext);
			var result = await bookingRepository.GetByIdAsync(nonExistingId, CancellationToken.None);


			// Assert
			Assert.Null(result);
		}


		[Fact]
		public async Task DeleteAsync_RetuntSucces()
		{
			// Arrange
			await ResetDatabaseAsync();
			var ct = CancellationToken.None;
			using var context = await CreateContextAsync();
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);
			var booking = Booking.Create(@event.Id);
			await context.Events.AddAsync(@event, ct);
			await context.Bookings.AddAsync(booking, ct);
			await context.SaveChangesAsync(ct);

			// Act
			using var verifyContext = await CreateContextAsync();
			var bookingRepository = new DbBookingRepository(context);
			var result = await bookingRepository.DeleteAsync(booking.Id, ct);
			var deletedBooking = await verifyContext.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id, ct);

			//Assert
			Assert.True(result);
			Assert.Null(deletedBooking);
		}
		[Fact]
		public async Task DeleteAsync_NonExistingBooking_ReturnFalse()
		{
			// Arrange
			await ResetDatabaseAsync();
			var ct = CancellationToken.None;
			var nonExistingId = Guid.NewGuid();
			using var context = await CreateContextAsync();
			var bookingRepository = new DbBookingRepository(context);

			// Act
			var result = await bookingRepository.DeleteAsync(nonExistingId, CancellationToken.None);

			// Assert
			Assert.False(result);
		}



		[Fact]
		public async Task UpdateAsync_ExistingBooking_ShouldUpdateStatus()
		{
			// Arrange
			await ResetDatabaseAsync();
			var ct = CancellationToken.None;
			using var context = await CreateContextAsync();
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);
			var booking = Booking.Create(@event.Id);

			await context.Events.AddAsync(@event, ct);
			await context.Bookings.AddAsync(booking, ct);
			await context.SaveChangesAsync(ct);

			// Act
			using var verifyContext = await CreateContextAsync();
			var bookingRepository = new DbBookingRepository(context);
			booking.Status = BookingStatus.Confirmed;
			booking.ProcessedAt = DateTime.UtcNow.AddHours(1);
			await bookingRepository.UpdateAsync(booking, ct);

			// Assert
			var result = await verifyContext.Bookings
					.FirstOrDefaultAsync(b => b.Id == booking.Id, ct);

			Assert.NotNull(result);
			Assert.Equal(BookingStatus.Confirmed, result.Status);
			Assert.NotNull(result.ProcessedAt);
		}


		[Fact]
		public async Task ListAsync_WithQuery_ShouldReturnFilteredBookings()
		{
			// Arrange
			await ResetDatabaseAsync();
			var ct = CancellationToken.None;
			using var context = await CreateContextAsync();
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);
			var booking = Booking.Create(@event.Id);
			booking.Status = BookingStatus.Rejected;
			var bookings = Enumerable.Range(0, 5).Select(i => Booking.Create(@event.Id));

			// Arrange
			await context.Events.AddAsync(@event, ct);
			await context.Bookings.AddAsync(booking, ct);
			await context.Bookings.AddRangeAsync(bookings, ct);
			await context.SaveChangesAsync(ct);

			// Act
			using var verifyContext = await CreateContextAsync();
			var bookingRepository = new DbBookingRepository(context);
			var result = await bookingRepository.ListAsync(b => b.Status == BookingStatus.Rejected, ct);

			var resultWithNullFilter = await bookingRepository.ListAsync(null, ct);

			// Assert
			Assert.Single(result);
			Assert.All(result, b => Assert.Equal(BookingStatus.Rejected, b.Status));

			Assert.Equal(6, resultWithNullFilter.Count);

		}

	}
}
