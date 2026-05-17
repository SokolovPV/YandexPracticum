using System.Linq.Expressions;
using EventsApi.DataAccess;
using EventsApi.Models.Domain;
using EventsApi.WebApi.Application.CustomException;
using EventsApi.WebApi.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Sdk;

namespace TestEventsApi
{
    public class BookingBackgroundServiceTest
    {
        [Fact]
        [Trait("Category", "сервис бронирования")]
        public async Task ProcessBookingAsync_UpdateStatus_Confirmed()
        {
            // Arrange
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var eventRepositoryMock = new Mock<IEventRepository>();
            var loggerBgMock = new Mock<ILogger<BookingBackgroundService>>();
            var loggerSvcMock = new Mock<ILogger<BookingService>>();
            var loggerEventMock = new Mock<ILogger<EventService>>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            var now = DateTime.Now;

            scopeFactoryMock
                .Setup(s => s.CreateScope())
                .Returns(scopeMock.Object);
            scopeMock
                .Setup(s => s.ServiceProvider)
                .Returns(serviceProviderMock.Object);
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IBookingRepository)))
                .Returns(bookingRepositoryMock.Object);
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IEventRepository)))
                .Returns(eventRepositoryMock.Object);

            var @event = Event.Create(
                    title: "тестовое событие",
                    startAt: now,
                    endAt: now.AddDays(1),
                    totalSeats: 3);
            Booking booking = Booking.Create(@event.Id);

            bookingRepositoryMock
                .Setup(r => r.ListAsync(It.IsAny<Expression<Func<Booking, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Booking> { booking });

            bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);

            eventRepositoryMock
                .Setup(r => r.GetByIdAsync(booking.EventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(@event);

            var backgroundService = new BookingBackgroundService(scopeFactoryMock.Object, loggerBgMock.Object);
            var bookingService = new BookingService(eventRepositoryMock.Object, bookingRepositoryMock.Object, loggerSvcMock.Object);
            var eventService = new EventService(eventRepositoryMock.Object, loggerEventMock.Object);
            using var cts = new CancellationTokenSource();

            // Act
            await backgroundService.StartAsync(cts.Token);

            // Ждем 3 сек чтобы бронь успела поменять статус
            await Task.Delay(TimeSpan.FromSeconds(3));

            cts.Cancel();

            await backgroundService.StopAsync(CancellationToken.None);

            // Assert
            var returnBooking = await bookingService.GetBookingByIdAsync(booking.Id, CancellationToken.None);
            var returnEvent = await eventService.GetEventAsync(@event.Id, CancellationToken.None);

            Assert.NotNull(returnBooking);
            Assert.Equal(BookingStatus.Confirmed, returnBooking.Status);

            bookingRepositoryMock.Verify(r => r.UpdateAsync(
                It.Is<Booking>(b => b.Id == booking.Id && b.Status == BookingStatus.Confirmed),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        [Trait("Category", "сервис бронирования")]
        public async Task ProcessBookingAsync_UpdateStatus_Rejected()
        {
            // Arrange
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var eventRepositoryMock = new Mock<IEventRepository>();
            var loggerBgMock = new Mock<ILogger<BookingBackgroundService>>();
            var loggerSvcMock = new Mock<ILogger<BookingService>>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            var now = DateTime.Now;

            scopeFactoryMock
                .Setup(s => s.CreateScope())
                .Returns(scopeMock.Object);
            scopeMock
                .Setup(s => s.ServiceProvider)
                .Returns(serviceProviderMock.Object);
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IBookingRepository)))
                .Returns(bookingRepositoryMock.Object);
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IEventRepository)))
                .Returns(eventRepositoryMock.Object);

            var booking = Booking.Create(Guid.NewGuid());
            bookingRepositoryMock
                .Setup(r => r.ListAsync(It.IsAny<Expression<Func<Booking, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Booking> { booking });

            bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);

            eventRepositoryMock
                .Setup(r => r.GetByIdAsync(booking.EventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => null);

            var backgroundService = new BookingBackgroundService(scopeFactoryMock.Object, loggerBgMock.Object);
            var bookingService = new BookingService(eventRepositoryMock.Object, bookingRepositoryMock.Object, loggerSvcMock.Object);
            using var cts = new CancellationTokenSource();
            // Act
            await backgroundService.StartAsync(cts.Token);

            // Ждем 3 сек чтобы бронь успела поменять статус
            //await Task.Delay(TimeSpan.FromSeconds(3), cts.Token);

            //cts.Cancel();

            await backgroundService.StopAsync(CancellationToken.None);

            // Assert
            var returnBooking = await bookingService.GetBookingByIdAsync(booking.Id, CancellationToken.None);

            Assert.NotNull(returnBooking);
            Assert.Equal(BookingStatus.Rejected, returnBooking.Status);

            bookingRepositoryMock.Verify(r => r.UpdateAsync(
                It.Is<Booking>(b => b.Id == booking.Id && b.Status == BookingStatus.Rejected),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        [Trait("Category", "ProcessBooking")]
        public async Task ProcessBookingAsync_RestoreAvailableSeats_WhenBookingIsRejected()
        {
            // Arrange
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var eventRepositoryMock = new Mock<IEventRepository>();
            var loggerBgMock = new Mock<ILogger<BookingBackgroundService>>();
            var loggerSvcMock = new Mock<ILogger<BookingService>>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            var now = DateTime.Now;
            var cts = new CancellationTokenSource();

            scopeFactoryMock
                .Setup(s => s.CreateScope())
                .Returns(scopeMock.Object);
            scopeMock
                .Setup(s => s.ServiceProvider)
                .Returns(serviceProviderMock.Object);
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IBookingRepository)))
                .Returns(bookingRepositoryMock.Object);
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IEventRepository)))
                .Returns(eventRepositoryMock.Object);

            var @event = Event.Create(
                    title: "тестовое событие",
                    startAt: now,
                    endAt: now.AddDays(1),
                    totalSeats: 3);
            @event.AvailableSeats = 2; // бронируем 1 место
            Booking booking = Booking.Create(@event.Id);

            bookingRepositoryMock
                .Setup(r => r.ListAsync(It.IsAny<Expression<Func<Booking, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Booking> { booking });

            bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);

            eventRepositoryMock
                .Setup(r => r.UpdateAsync(@event, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            //.Throws<InvalidOperationException>();

            eventRepositoryMock
                .Setup(r => r.GetByIdAsync(booking.EventId, It.IsAny<CancellationToken>()))
                //.Throws<InvalidOperationException>();
                .ReturnsAsync(@event);


            var backgroundService = new BookingBackgroundService(scopeFactoryMock.Object, loggerBgMock.Object);
            var bookingService = new BookingService(eventRepositoryMock.Object, bookingRepositoryMock.Object, loggerSvcMock.Object);

            //await bookingService.CreateBookingAsync(@event.Id, cts.Token);
            var AvailableSeatsBeforProcessing = @event.AvailableSeats;



            // Act
            await backgroundService.StartAsync(cts.Token);


            // Ждем 3 сек
            await Task.Delay(3000);
            await backgroundService.StopAsync(cts.Token);

            // Assert
            var returnBooking = await bookingService.GetBookingByIdAsync(booking.Id, CancellationToken.None);

            Assert.NotNull(returnBooking);
            Assert.Equal(2, AvailableSeatsBeforProcessing); // т.к. до catch одно место заброниловась
            Assert.Equal(BookingStatus.Rejected, returnBooking.Status);
            Assert.Equal(@event.TotalSeats, @event.AvailableSeats);

            bookingRepositoryMock.Verify(r => r.UpdateAsync(
                It.Is<Booking>(b => b.Id == booking.Id && b.Status == BookingStatus.Rejected),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        [Trait("Category", "ProcessBooking")]
        public async Task CreateBookingAsync_OverbookingProtection()
        {
            // Arrange
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var eventRepositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var bookingService = new BookingService(eventRepositoryMock.Object, bookingRepositoryMock.Object, loggerMock.Object);
            var now = DateTime.Now;
            var totalSeats = 5;
            var countTask = 20;
            var ct = CancellationToken.None;

            var @event = Event.Create(
                    title: "тестовое событие",
                    startAt: now,
                    endAt: now.AddDays(1),
                    totalSeats: totalSeats);


            eventRepositoryMock
                .Setup(s => s.GetByIdAsync(@event.Id, ct))
                .ReturnsAsync(@event);

            var tasks = Enumerable.Range(0, countTask)
                .Select(_ => bookingService.CreateBookingAsync(@event.Id, ct))
                .ToList();


            var results = new List<Booking>();
            var exceptions = new List<Exception>();

            //Act
            while (tasks.Count > 0)
            {
                Task<Booking>? completed = null;
                try
                {
                    completed = await Task.WhenAny(tasks);
                    results.Add(await completed);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
                finally
                {
                    tasks.Remove(completed!);
                }
            }

            // Assert
            var countResult = results.Count;
            var countEx_where_NoAvailableSeatsException = exceptions.Count(q => q is NoAvailableSeatsException);

            Assert.Equal(totalSeats, countResult);
            Assert.Equal(0, @event.AvailableSeats); //заняли все места
            Assert.Equal(15, countEx_where_NoAvailableSeatsException);

        }


        [Fact]
        [Trait("Category", "ProcessBooking")]
        public async Task CreateBookingAsync_Concurren10Requests_GenerateUniqueIds()
        {
            // Arrange
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var eventRepositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var bookingService = new BookingService(eventRepositoryMock.Object, bookingRepositoryMock.Object, loggerMock.Object);
            var now = DateTime.Now;
            var totalSeats = 10;
            var countTask = 10;
            var ct = CancellationToken.None;

            var @event = Event.Create(
                    title: "тестовое событие",
                    startAt: now,
                    endAt: now.AddDays(1),
                    totalSeats: totalSeats);


            eventRepositoryMock
                .Setup(s => s.GetByIdAsync(@event.Id, ct))
                .ReturnsAsync(@event);

            var tasks = Enumerable.Range(0, countTask)
                .Select(_ => bookingService.CreateBookingAsync(@event.Id, ct))
                .ToList();


            var results = new List<Booking>();
            var exceptions = new List<Exception>();

            //Act
            while (tasks.Count > 0)
            {
                Task<Booking>? completed = null;
                try
                {
                    completed = await Task.WhenAny(tasks);
                    results.Add(await completed);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
                finally
                {
                    tasks.Remove(completed!);
                }
            }
            var countUniqueIds = results.Select(q => q.Id).Distinct().Count();
            // Assert
            Assert.Equal(totalSeats, countUniqueIds);
            Assert.Equal(totalSeats, results.Count);
            Assert.Empty(exceptions);
            Assert.Equal(0, @event.AvailableSeats); //заняли все места
        }

    }
}