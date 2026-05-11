
using EventsApi.Application.Interfaces;
using EventsApi.Application.Services;
using EventsApi.Infrastructure.Interfaces;
using EventsApi.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Linq.Expressions;


namespace TestEventsApi
{
    public class BookingServiceTests //: IDisposable
    {
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;
    private readonly IEventRepository _eventRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingService> _bookingLogger;
    private readonly ILogger<EventService> _eventLogger;

    public BookingServiceTests()
    {
      var dbName = Guid.NewGuid().ToString();
      var services = new ServiceCollection();
      services.AddDbContext<AppDbContext>(options =>
          options.UseInMemoryDatabase(dbName));
      services.AddScoped<IEventRepository, DbEventRepository>();
      services.AddScoped<IBookingRepository, DbBookingRepository>();
      services.AddScoped<IEventService, EventService>();
      services.AddScoped<IBookingService, BookingService>();
      services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));


      _serviceProvider = services.BuildServiceProvider();
      _scope = _serviceProvider.CreateScope();
      _eventRepository = _scope.ServiceProvider.GetRequiredService<IEventRepository>();
      _bookingRepository = _scope.ServiceProvider.GetRequiredService<IBookingRepository>();
      _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();
      _bookingService = _scope.ServiceProvider.GetRequiredService<IBookingService>();
      _bookingLogger = _scope.ServiceProvider.GetRequiredService<ILogger<BookingService>>();
      _eventLogger = _scope.ServiceProvider.GetRequiredService<ILogger<EventService>>();
    }
    public void Dispose()
    {
      _scope.Dispose();
      _serviceProvider.Dispose();
    }

    #region создание бронирования

    // [Fact]
    // public async Task CreateBookingAsync_WithValidEventId_ReturnsBookingInfoWithPendingStatus()
    // {
    //     var _event = Event.Create(title: "Test",
    //                                    description: String.Empty,
    //                                    startAt: DateTime.Now,
    //                                    endAt: DateTime.Now.AddHours(1),
    //                                    totalSeats: 1);
    //     var result = await _bookingService.CreateBookingAsync(_event.Id, CancellationToken.None);

    //     Assert.NotNull(result);
    //     Assert.NotEqual(Guid.Empty, result.Id);
    //     Assert.Equal(_event.Id, result.EventId);
    //     Assert.Equal(BookingStatus.Pending, result.Status);
    //     Assert.Null(result.ProcessedAt);
    // }


    [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task CreateBookingAsync_Success()
        {
            // Arrange
            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var bookingService = new BookingService(eventRepositoryMock.Object, bookingRepositoryMock.Object, loggerMock.Object);
            var ct = CancellationToken.None;
            var _event = Event.Create(title: "Test",
                                        description: String.Empty,
                                        startAt: DateTime.Now,
                                        endAt: DateTime.Now.AddHours(1),
                                        totalSeats: 1);
            bookingRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()));

            eventRepositoryMock
                .Setup(s => s.GetByIdAsync(_event.Id, ct))
                .ReturnsAsync(_event);

            // Act
            var result = await bookingService.CreateBookingAsync(_event.Id, ct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(BookingStatus.Pending, result.Status);
            Assert.Equal(_event.Id, result.EventId);
            bookingRepositoryMock.Verify(r => r.AddAsync(It.Is<Booking>(b => b.EventId == _event.Id), ct), Times.Once);
            eventRepositoryMock.Verify(s => s.GetByIdAsync(_event.Id, ct), Times.Once);
        }

        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task CreateBookingAsync_AddManyBookings_CheckAvailableSeats_Success()
        {
            // Arrange
            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var bookingService = new BookingService(eventRepositoryMock.Object, bookingRepositoryMock.Object, loggerMock.Object);
            var ct = CancellationToken.None;
            var _event = Event.Create(title: "Test",
                                        description: string.Empty,
                                        startAt: DateTime.Now,
                                        endAt: DateTime.Now.AddHours(5),
                                        totalSeats: 10);

            bookingRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()));

            eventRepositoryMock
                .Setup(s => s.GetByIdAsync(_event.Id, ct))
                .ReturnsAsync(_event);

            int count = 10;
            var createdIds = new HashSet<Guid>();
            var eventIds = new List<Guid>();
            var returnEventId = _event.Id;
            // Act
            for (int i = 0; i < count; i++)
            {
                var result = await bookingService.CreateBookingAsync(_event.Id, ct);
                createdIds.Add(result.Id);
                eventIds.Add(result.EventId);
            }

            // Assert
            Assert.NotEmpty(createdIds);
            Assert.Equal(10, createdIds.Count); // проверяем что создаются уникальные идентификаторы бронирования для одного события
            Assert.All(eventIds, q => Assert.True(q == _event.Id)); // проверяем - что для всех новых бронирований идентификатор мероприятия не изменился
            Assert.Equal(0, _event.AvailableSeats); // проверяем - что все места на мероприятие заняли
            bookingRepositoryMock.Verify(r => r.AddAsync(It.Is<Booking>(b => b.EventId == _event.Id), ct), Times.Exactly(count));
            eventRepositoryMock.Verify(s => s.GetByIdAsync(_event.Id, ct), Times.Exactly(count));
        }

        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task CreateBookingAsync_ThrowKeyNotExistException()
        {
            // Arrange
            var eventService = new Mock<IEventService>();
            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var loggerBookingMock = new Mock<ILogger<BookingService>>();
            var loggerEventMock = new Mock<ILogger<EventService>>();
            var bookingService = new BookingService(eventRepositoryMock.Object, bookingRepositoryMock.Object, loggerBookingMock.Object);
            var notExistedEventId = Guid.NewGuid();
            var ct = CancellationToken.None;
            bookingRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()));

            eventService
                .Setup(s => s.GetEventAsync(notExistedEventId, ct))
                .ThrowsAsync(new KeyNotExistException(notExistedEventId, "test"));

            // Act Assert
            await Assert.ThrowsAsync<KeyNotExistException>(async () => await bookingService.CreateBookingAsync(notExistedEventId, ct));
            bookingRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task CreateBookingAsync_ThrowOperationCanceledException()
        {
            // Arrange
            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var bookingService = new BookingService(eventRepositoryMock.Object, bookingRepositoryMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            bookingRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()));

            // Act Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await bookingService.CreateBookingAsync(Guid.NewGuid(), cts.Token));
            bookingRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task CreateBookingAsync_ThrowNoAvailableSeatsException()
        {
            // Arrange
            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var bookingService = new BookingService(eventRepositoryMock.Object, bookingRepositoryMock.Object, loggerMock.Object);
            var ct = CancellationToken.None;
            var totalSeats = 3;
            var _event = Event.Create(title: "Test",
                                        description: string.Empty,
                                        startAt: DateTime.Now,
                                        endAt: DateTime.Now.AddHours(5),
                                        totalSeats: totalSeats); // 3 доступных мест для бронирования

            bookingRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()));

            eventRepositoryMock
                .Setup(s => s.GetByIdAsync(_event.Id, ct))
                .ReturnsAsync(_event);

            // Act
            // бронируем все мества
            for (int i = 0; i < totalSeats; i++)
            {
                var result_1 = await bookingService.CreateBookingAsync(_event.Id, ct);
            }

            // Act Assert
            Assert.Equal(0, _event.AvailableSeats);
            // при повторном бронирование на событие где 0 доступных мест - ошибка NoAvailableSeatsException
            await Assert.ThrowsAsync<NoAvailableSeatsException>(async () => await bookingService.CreateBookingAsync(_event.Id, ct));
            bookingRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.AtLeast(3));
        }



        #endregion


        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task GetBookingByIdAsync_BookingId_ThrowKeyNotExistException()
        {
            // Arrange
            var eventRepositoryMock = new Mock<IEventRepository>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var bookingService = new BookingService(eventRepositoryMock.Object, repositoryMock.Object, loggerMock.Object);

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
            var eventRepositoryMock = new Mock<IEventRepository>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var bookingService = new BookingService(eventRepositoryMock.Object, repositoryMock.Object, loggerMock.Object);
            var booking = Booking.Create(Guid.NewGuid());
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
            var eventRepositoryMock = new Mock<IEventRepository>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var bookingService = new BookingService(eventRepositoryMock.Object, repositoryMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()));

            // Act Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await bookingService.GetBookingByIdAsync(Guid.NewGuid(), cts.Token));
            repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        }


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
            await Task.Delay(TimeSpan.FromSeconds(3));

            cts.Cancel();

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

            eventRepositoryMock // эмулируем ошибку - что бы поппасть в блок catch 
                .Setup(r => r.UpdateAsync(@event, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            //.Throws<InvalidOperationException>();

            eventRepositoryMock // эмулируем ошибку - что бы поппасть в блок catch 
                .Setup(r => r.GetByIdAsync(booking.EventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(@event);

            eventRepositoryMock
                .Setup(r => r.GetByIdAsync(booking.EventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(@event);

            var backgroundService = new BookingBackgroundService(scopeFactoryMock.Object, loggerBgMock.Object);
            var bookingService = new BookingService(eventRepositoryMock.Object, bookingRepositoryMock.Object, loggerSvcMock.Object);
            using var cts = new CancellationTokenSource();
            await bookingService.CreateBookingAsync(@event.Id, cts.Token);
            var AvailableSeatsBeforProcessing = @event.AvailableSeats;



            // Act
            await backgroundService.StartAsync(cts.Token);


            // Ждем 1 сек и отменяем операцию что бы вернуть места
            await Task.Delay(1000);
            cts.Cancel();

            await backgroundService.StopAsync(CancellationToken.None);

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
