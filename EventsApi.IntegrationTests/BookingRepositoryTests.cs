using EventsApi.Domain.Entities;
using EventsApi.Domain.Enums;
using EventsApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EventsApi.IntegrationTests
{

	public class BookingRepositoryTests : IClassFixture<PostgreSqlFixture>
	{
		private readonly PostgreSqlFixture postgreSqlFixture;
		public BookingRepositoryTests(PostgreSqlFixture _postgreSqlFixture) => postgreSqlFixture = _postgreSqlFixture;


		[Fact]
		public async Task AddAsync_SaveBookingToDatabase()
		{
			// Arrange
			await postgreSqlFixture.ResetDatabaseAsync();
			using var context = await postgreSqlFixture.CreateContextAsync();

			var bookingRepository = new DbBookingRepository(context);
			var eventRepository = new DbEventRepository(context);
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);
			await eventRepository.AddAsync(@event, CancellationToken.None);
			var booking = Booking.Create(@event.Id, Guid.NewGuid());

			//Act
			await bookingRepository.AddAsync(booking, CancellationToken.None);

			// Assert — читаем из реальной БД через отдельный контекст
			using var verifyContext = await postgreSqlFixture.CreateContextAsync();
			var saved = await verifyContext.Bookings
					.FirstOrDefaultAsync(b => b.Id == booking.Id, CancellationToken.None);

			Assert.NotNull(saved);
			Assert.Equal(booking.Id, saved.Id);
		}
		[Fact]
		public async Task AddAsync_WithNotExistEvent_ReturnsThrowDbUpdateException()
		{
			// Arrange
			await postgreSqlFixture.ResetDatabaseAsync();
			using var context = await postgreSqlFixture.CreateContextAsync();

			var bookingRepository = new DbBookingRepository(context);
			var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());


			// Act Assert
			await Assert.ThrowsAsync<DbUpdateException>(async () => await bookingRepository.AddAsync(booking, CancellationToken.None));
		}


		[Fact]
		public async Task GetByIdAsync_ExistingBooking_ShouldReturnBooking()
		{
			// Arrange
			await postgreSqlFixture.ResetDatabaseAsync();
			var ct = CancellationToken.None;
			using var context = await postgreSqlFixture.CreateContextAsync();
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);
			var booking = Booking.Create(@event.Id, Guid.NewGuid());
			await context.Events.AddAsync(@event, ct);
			await context.Bookings.AddAsync(booking, ct);
			await context.SaveChangesAsync(ct);

			// Act
			using var verifyContext = await postgreSqlFixture.CreateContextAsync();
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
			using var verifyContext = await postgreSqlFixture.CreateContextAsync();
			var bookingRepository = new DbBookingRepository(verifyContext);
			var result = await bookingRepository.GetByIdAsync(nonExistingId, CancellationToken.None);


			// Assert
			Assert.Null(result);
		}


		[Fact]
		public async Task DeleteAsync_RetuntSucces()
		{
			// Arrange
			await postgreSqlFixture.ResetDatabaseAsync();
			var ct = CancellationToken.None;
			using var context = await postgreSqlFixture.CreateContextAsync();
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);
			var booking = Booking.Create(@event.Id, Guid.NewGuid());
			await context.Events.AddAsync(@event, ct);
			await context.Bookings.AddAsync(booking, ct);
			await context.SaveChangesAsync(ct);

			// Act
			using var verifyContext = await postgreSqlFixture.CreateContextAsync();
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
			await postgreSqlFixture.ResetDatabaseAsync();
			var ct = CancellationToken.None;
			var nonExistingId = Guid.NewGuid();
			using var context = await postgreSqlFixture.CreateContextAsync();
			var bookingRepository = new DbBookingRepository(context);

			// Act
			var result = await bookingRepository.DeleteAsync(nonExistingId, ct);

			// Assert
			Assert.False(result);
		}



		[Fact]
		public async Task UpdateAsync_ExistingBooking_ShouldUpdateStatus()
		{
			// Arrange
			await postgreSqlFixture.ResetDatabaseAsync();
			var ct = CancellationToken.None;
			using var context = await postgreSqlFixture.CreateContextAsync();
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);
			var booking = Booking.Create(@event.Id, Guid.NewGuid());

			await context.Events.AddAsync(@event, ct);
			await context.Bookings.AddAsync(booking, ct);
			await context.SaveChangesAsync(ct);

			// Act
			using var verifyContext = await postgreSqlFixture.CreateContextAsync();
			var bookingRepository = new DbBookingRepository(context);
			booking.Confirm();
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
			await postgreSqlFixture.ResetDatabaseAsync();
			var ct = CancellationToken.None;
			using var context = await postgreSqlFixture.CreateContextAsync();
			var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 5);
			var booking = Booking.Create(@event.Id, Guid.NewGuid());
			booking.Reject();
			var bookings = Enumerable.Range(0, 5).Select(i => Booking.Create(@event.Id, Guid.NewGuid()));

			// Arrange
			await context.Events.AddAsync(@event, ct);
			await context.Bookings.AddAsync(booking, ct);
			await context.Bookings.AddRangeAsync(bookings, ct);
			await context.SaveChangesAsync(ct);

			// Act
			using var verifyContext = await postgreSqlFixture.CreateContextAsync();
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
