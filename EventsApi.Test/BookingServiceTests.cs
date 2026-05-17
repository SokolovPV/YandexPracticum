
using EventsApi.DataAccess;
using EventsApi.Models.Domain;
using EventsApi.WebApi.Application.CustomException;
using EventsApi.WebApi.Application.Interfaces;
using EventsApi.WebApi.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Linq.Expressions;


namespace TestEventsApi
{
    public class BookingServiceTests : IDisposable
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

        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task CreateBookingAsync_Success()
        {
            // Arrange
            var ct = CancellationToken.None;
            var _event = Event.Create(title: "Test",
                                        description: String.Empty,
                                        startAt: DateTime.Now,
                                        endAt: DateTime.Now.AddHours(1),
                                        totalSeats: 1);
            await _eventRepository.AddAsync(_event, ct);

            // Act
            var result = await _bookingService.CreateBookingAsync(_event.Id, ct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(BookingStatus.Pending, result.Status);
            Assert.Equal(_event.Id, result.EventId);
        }

        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task CreateBookingAsync_WithManyBookings_CheckAvailableSeats_Success()
        {
            // Arrange
            var ct = CancellationToken.None;
            var _event = Event.Create(title: "Test",
                                        description: string.Empty,
                                        startAt: DateTime.Now,
                                        endAt: DateTime.Now.AddHours(5),
                                        totalSeats: 10);

            await _eventRepository.AddAsync(_event, ct);
            int count = 10;
            var createdIds = new HashSet<Guid>();
            var eventIds = new List<Guid>();
            var returnEventId = _event.Id;
            // Act
            for (int i = 0; i < count; i++)
            {
                var result = await _bookingService.CreateBookingAsync(_event.Id, ct);
                createdIds.Add(result.Id);
                eventIds.Add(result.EventId);
            }

            // Assert
            Assert.NotEmpty(createdIds);
            Assert.Equal(10, createdIds.Count); // проверяем что создаются уникальные идентификаторы бронирования для одного события
            Assert.All(eventIds, q => Assert.True(q == _event.Id)); // проверяем - что для всех новых бронирований идентификатор мероприятия не изменился
            Assert.Equal(0, _event.AvailableSeats); // проверяем - что все места на мероприятие заняли
        }

        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task CreateBookingAsync_ThrowKeyNotExistException()
        {
            // Arrange
            var notExistedEventId = Guid.NewGuid();
            var ct = CancellationToken.None;
            // Act Assert
            await Assert.ThrowsAsync<KeyNotExistException>(async () => await _bookingService.CreateBookingAsync(notExistedEventId, ct));
        }

        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task CreateBookingAsync_ThrowOperationCanceledException()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await _bookingService.CreateBookingAsync(Guid.NewGuid(), cts.Token));
        }

        [Fact]
        [Trait("Category", "создание бронирования")]
        public async Task CreateBookingAsync_ThrowNoAvailableSeatsException()
        {
            // Arrange
            var ct = CancellationToken.None;
            var totalSeats = 3;
            var _event = Event.Create(title: "Test",
                                        description: string.Empty,
                                        startAt: DateTime.Now,
                                        endAt: DateTime.Now.AddHours(5),
                                        totalSeats: totalSeats); // 3 доступных мест для бронирования

            await _eventRepository.AddAsync(_event, ct);
            // Act
            // бронируем все мества
            for (int i = 0; i < totalSeats; i++)
            {
                await _bookingService.CreateBookingAsync(_event.Id, ct);
            }

            // Act Assert
            Assert.Equal(0, _event.AvailableSeats);
            // при повторном бронирование на событие где 0 доступных мест - ошибка NoAvailableSeatsException
            await Assert.ThrowsAsync<NoAvailableSeatsException>(async () => await _bookingService.CreateBookingAsync(_event.Id, ct));
        }

        #endregion

        #region создание бронирования


        [Fact]
        [Trait("Category", "получение бронирования")]
        public async Task GetBookingByIdAsync_ThrowKeyNotExistException()
        {
            // Arrange
            var notExistedBookingId = Guid.NewGuid();
            var ct = CancellationToken.None;

            // Act Assert
            await Assert.ThrowsAsync<KeyNotExistException>(async () => await _bookingService.GetBookingByIdAsync(notExistedBookingId, ct)); // Проверка наличия ID в сообщении (если есть)
        }

        [Fact]
        [Trait("Category", "получение бронирования")]
        public async Task GetBookingByIdAsync_Success()
        {
            // Arrange
            var ct = CancellationToken.None;
            var booking = Booking.Create(Guid.NewGuid());
            await _bookingRepository.AddAsync(booking, ct);
            // Act
            var result = await _bookingService.GetBookingByIdAsync(booking.Id, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.Id, booking.Id);
        }

        [Fact]
        [Trait("Category", "получение бронирования")]
        public async Task GetBookingByIdAsync_ThrowIfCancelled()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await _bookingService.GetBookingByIdAsync(Guid.NewGuid(), cts.Token));
        }

        #endregion
    }
}
