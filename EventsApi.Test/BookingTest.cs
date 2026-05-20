
using EventsApi.Models.Domain;

namespace TestEventsApi
{
    public class BookingTests
    {
        [Fact]
        public async Task Booking_Confirm()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var booking = Booking.Create(eventId);

            //Act
            booking.Confirm();

            //Assert
            Assert.Equal(eventId, booking.EventId);
            Assert.NotNull(booking.ProcessedAt);
            Assert.Equal(BookingStatus.Confirmed, booking.Status);

        }

        [Fact]
        public async Task Booking_Reject()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var booking = Booking.Create(eventId);

            //Act
            booking.Reject();

            //Assert
            Assert.Equal(eventId, booking.EventId);
            Assert.NotNull(booking.ProcessedAt);
            Assert.Equal(BookingStatus.Rejected, booking.Status);
        }

    }
}