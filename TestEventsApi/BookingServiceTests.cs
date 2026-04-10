using EventsApi.Application.CustomException;
using EventsApi.Application.Interfaces;
using EventsApi.Application.Services;
using EventsApi.Infrastructure.Interfaces;
using EventsApi.Models.Domain;
using EventsApi.Models.ModelDTO.Event;
using Microsoft.Extensions.DependencyInjection;
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
            var bookingService = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);
            var eventId = Guid.NewGuid();
            var ct = CancellationToken.None;
            var eventDto = new ResponseEventDTO(Id: eventId, Title: "Test", Description: String.Empty, StartAt: DateTime.Now, EndAt: DateTime.Now.AddHours(1));
            repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()));

            eventServiceMock
                .Setup(s => s.GetEventAsync(eventId, ct))
                .ReturnsAsync(eventDto);

            // Act
            var result = await bookingService.CreateBookingAsync(eventId, ct);

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
            var bookingService = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);
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
                var result = await bookingService.CreateBookingAsync(eventId, ct);
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
            var bookingService = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);
            var notExistedEventId = Guid.NewGuid();
            var ct = CancellationToken.None;
            repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()));

            eventServiceMock
                .Setup(s => s.GetEventAsync(notExistedEventId, ct))
                .ThrowsAsync(new KeyNotExistException(notExistedEventId, "test"));

            // Act Assert
            await Assert.ThrowsAsync<KeyNotExistException>(async () => await bookingService.CreateBookingAsync(notExistedEventId, ct));
            repositoryMock.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task CreateBookingAsync_ThrowOperationCanceledException()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var bookingService = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            repositoryMock.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()));

            // Act Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await bookingService.CreateBookingAsync(Guid.NewGuid(), cts.Token));
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
            var bookingService = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);

            var notExistedBookingId = Guid.NewGuid();
            var ct = CancellationToken.None;

            repositoryMock
                .Setup(r => r.GetByIdAsync(notExistedBookingId, ct))
                .ReturnsAsync((Booking?)null);

            // Act Assert
            await Assert.ThrowsAsync<KeyNotExistException>(async () => await bookingService.GetBookingByIdAsync(notExistedBookingId, ct)); // Проверка наличия ID в сообщении (если есть)

            repositoryMock.Verify(r => r.GetByIdAsync(notExistedBookingId, ct), Times.Once);
        }

        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task GetBookingByIdAsync_Success()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var bookingService = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);
            var booking = new Booking(Guid.NewGuid());
            repositoryMock
                .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);

            // Act
            var result = await bookingService.GetBookingByIdAsync(booking.Id, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.Id, booking.Id);
            repositoryMock.Verify(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task GetBookingByIdAsync_ThrowIfCancelled()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var bookingService = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()));

            // Act Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await bookingService.GetBookingByIdAsync(Guid.NewGuid(), cts.Token));
            repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        }


        [Fact]
        public async Task ExecuteAsync_UpdateStatus()
        {
            // Arrange
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerBgMock = new Mock<ILogger<BookingBackgroundService>>();
            var loggerSvcMock = new Mock<ILogger<BookingService>>();
            var eventServiceMock = new Mock<IEventService>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();

            scopeFactoryMock
                .Setup(s => s.CreateScope())
                .Returns(scopeMock.Object);
            scopeMock
                .Setup(s => s.ServiceProvider)
                .Returns(serviceProviderMock.Object);
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IBookingRepository)))
                .Returns(repositoryMock.Object);

            var booking = new Booking(Guid.NewGuid());

            repositoryMock
                .Setup(r => r.ListAsync(It.IsAny<Func<Booking, bool>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Booking> { booking });

            repositoryMock
                .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);

            var backgroundService = new BookingBackgroundService(scopeFactoryMock.Object, loggerBgMock.Object);
            var bookingService = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerSvcMock.Object);
            using var cts = new CancellationTokenSource();

            // Act
            await backgroundService.StartAsync(cts.Token);

            // Ждем 3 сек чтобы бронь успела поменять статус
            await Task.Delay(TimeSpan.FromSeconds(3));

            cts.Cancel();

            await backgroundService.StopAsync(CancellationToken.None);

            // Assert
            var resultDto = await bookingService.GetBookingByIdAsync(booking.Id, CancellationToken.None);

            Assert.NotNull(resultDto);
            Assert.Equal(BookingStatus.Confirmed.ToString(), resultDto.Status);

            repositoryMock.Verify(r => r.UpdateAsync(
                It.Is<Booking>(b => b.Id == booking.Id && b.Status == BookingStatus.Confirmed),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
