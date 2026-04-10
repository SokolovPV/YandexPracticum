using EventsApi.Application.CustomException;
using EventsApi.Application.Interfaces;
using EventsApi.Application.Services;
using EventsApi.Infrastructure.Interfaces;
using EventsApi.Models.Domain;
using EventsApi.Models.ModelDTO.Event;
using Microsoft.Extensions.Logging;
using Moq;


namespace TestEventsApi
{
    public class BookingServiceTests
    {
        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task CreateBookingAsync_Success()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var service = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);
            var eventId = Guid.NewGuid();
            var ct = CancellationToken.None;
            var eventDto = new ResponseEventDTO(Id: eventId, Title: "Test", Description: String.Empty, StartAt: DateTime.Now, EndAt: DateTime.Now.AddHours(1));
            repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()));

            eventServiceMock
                .Setup(s => s.GetEventAsync(eventId, ct))
                .ReturnsAsync(eventDto);

            // Act
            var result = await service.CreateBookingAsync(eventId, ct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(BookingStatus.Pending.ToString(), result.Status);
            Assert.Equal(eventId, result.EventID);
            repositoryMock.Verify(r => r.AddAsync(It.Is<Booking>(b => b.EventId == eventId), ct), Times.Once);
            eventServiceMock.Verify(s => s.GetEventAsync(eventId, ct), Times.Once);
        }

        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task CreateBookingAsync_AddManyBookings()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var service = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);
            var eventId = Guid.NewGuid();
            var ct = CancellationToken.None;
            var eventDto = new ResponseEventDTO(Id: eventId, Title: "Test Event", Description: String.Empty, StartAt: DateTime.Now, EndAt: DateTime.Now.AddHours(5));
            repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()));

            eventServiceMock
                .Setup(s => s.GetEventAsync(eventId, ct))
                .ReturnsAsync(eventDto);

            int count = 10;
            var createdIds = new HashSet<Guid>();
            var returnEventId = eventId;
            // Act
            for (int i = 0; i < count; i++)
            {
                var result = await service.CreateBookingAsync(eventId, ct);
                createdIds.Add(result.Id);
                returnEventId = returnEventId != result.EventID ? result.EventID : returnEventId;
            }

            // Assert
            Assert.NotEmpty(createdIds);
            Assert.Equal(10, createdIds.Count); // проверяем что создаются уникальные идентификаторы бронирования для одного события
            Assert.Equal(eventId, returnEventId); // проверяем  что для всех новых бронирований идентификатор мероприятия не изменился
            repositoryMock.Verify(r => r.AddAsync(It.Is<Booking>(b => b.EventId == eventId), ct), Times.Exactly(count));
            eventServiceMock.Verify(s => s.GetEventAsync(eventId, ct), Times.Exactly(count));
        }

        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task CreateBookingAsync_EventId_ThrowKeyNotExistException()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var service = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);
            var notExistedEventId = Guid.NewGuid();
            var ct = CancellationToken.None;
            repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()));

            eventServiceMock
                .Setup(s => s.GetEventAsync(notExistedEventId, ct))
                .ThrowsAsync(new KeyNotExistException(notExistedEventId, "test"));

            // Act Assert
            await Assert.ThrowsAsync<KeyNotExistException>(async () => await service.CreateBookingAsync(notExistedEventId, ct));
            repositoryMock.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task GetBookingByIdAsync_BookingId_ThrowKeyNotExistException()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var service = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);

            var notExistedBookingId = Guid.NewGuid();
            var ct = CancellationToken.None;

            repositoryMock
                .Setup(r => r.GetByIdAsync(notExistedBookingId, ct))
                .ReturnsAsync((Booking?)null);

            // Act Assert
            // Проверяем, что выброшено именно NotFoundException
            await Assert.ThrowsAsync<KeyNotExistException>(async () => await service.GetBookingByIdAsync(notExistedBookingId, ct)); // Проверка наличия ID в сообщении (если есть)

            // Проверяем, что репозиторий действительно опрашивался
            repositoryMock.Verify(r => r.GetByIdAsync(notExistedBookingId, ct), Times.Once);
        }

        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task CreateBookingAsync_ThrowOperationCanceledException()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var service = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            repositoryMock.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()));

            // Act Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await service.CreateBookingAsync(Guid.NewGuid(), cts.Token));
            repositoryMock.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        }




    }
}
