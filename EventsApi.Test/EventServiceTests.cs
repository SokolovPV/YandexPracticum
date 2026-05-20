using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using EventsApi.DataAccess;
using EventsApi.Models.Domain;
using EventsApi.Models.ModelDTO.Event;
using EventsApi.WebApi.Application.CustomException;
using EventsApi.WebApi.Application.Interfaces;
using EventsApi.WebApi.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace TestEventsApi
{
    public class EventServiceTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IServiceScope _scope;
        private readonly IEventService _eventService;

        public EventServiceTests()
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
            _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();
        }
        public void Dispose()
        {
            _scope.Dispose();
            _serviceProvider.Dispose();
        }


        #region тесты: создание события

        [Fact]
        public async Task CreateEventAsync_WithValidData_ReturnEventInfoDto()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createEventDto = GetCreateEventDto(title: "Test Event", description: "Test Description", startAt: futureDate, endAt: futureDate.AddHours(2));
            //Act 
            var result = await _eventService.CreateEventAsync(createEventDto, CancellationToken.None);
            //Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("Test Event", result.Title);
            Assert.Equal("Test Description", result.Description);
            Assert.Equal(futureDate, result.StartAt);
            Assert.Equal(futureDate.AddHours(2), result.EndAt);
        }

        [Fact]
        [Trait("Category", " создание события")]
        public async Task CreateEventAsync_WithDateFromMoreThanDateTo_ReturnThrowValidationException()
        {
            // Arrange
            var createEventDto = GetCreateEventDto(title: "Тестовое событие с невалидной моделью данных", startAt: DateTime.Now.AddHours(2), endAt: DateTime.Now.AddHours(1));// Конец позже начала

            //Act  Assert
            await Assert.ThrowsAsync<ValidationException>(async () => await _eventService.CreateEventAsync(createEventDto, CancellationToken.None));
        }

        [Fact]
        [Trait("Category", " создание события")]
        public async Task CreateEventAsync_WithTotalSeatsMoreThanRange_ReturnThrowValidationException()
        {
            // Arrange
            var dto = GetCreateEventDto(title: "Тестовое событие с невалидной моделью данных", startAt: DateTime.Now.AddHours(1), endAt: DateTime.Now.AddHours(2), totalSeats: 200); // больше 100
            //Act  Assert
            await Assert.ThrowsAsync<ValidationException>(async () => await _eventService.CreateEventAsync(dto, CancellationToken.None));
        }


        [Fact]
        [Trait("Category", " создание события")]
        public async Task CreateEventAsync_WithTotalSeatsLessThanRange_ReturnThrowValidationException()
        {
            // Arrange
            var dto = GetCreateEventDto(title: "Тестовое событие с невалидной моделью данных", startAt: DateTime.Now.AddHours(1), endAt: DateTime.Now.AddHours(2), totalSeats: 0); // меньше 1
            //Act  Assert
            await Assert.ThrowsAsync<ValidationException>(async () => await _eventService.CreateEventAsync(dto, CancellationToken.None));
        }


        [Fact]
        [Trait("Category", " создание события")]
        public async Task CreateEventAsync_ReturnThrowOperationCanceledException()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var dto = GetCreateEventDto(title: "Тестовое событие", startAt: DateTime.Now.AddHours(1), endAt: DateTime.Now.AddHours(2));
            // Act Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await _eventService.CreateEventAsync(dto, cts.Token));
        }

        #endregion


        #region тесты: получение событий

        [Fact]
        [Trait("Category", "получение событий")]
        public async Task GetEventsAsync_ReturnsPaginatedResult()
        {
            // Arrange
            var filter = new EventsFilter();
            var now = DateTime.Now;
            var ct = CancellationToken.None;
            for (var i = 0; i < 3; i++)
            {
                var createEventDto = GetCreateEventDto(title: $"Тестовое событие {i}", startAt: now.AddHours(i + 1), endAt: now.AddHours(i + 2));
                await _eventService.CreateEventAsync(createEventDto, ct);
            }
            // Act
            var result = await _eventService.GetEventsAsync(filter, ct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalItems);
        }

        [Fact]
        [Trait("Category", "получение событий")]
        public async Task GetEventsAsync_WithFilterByName_ReturnsPaginatedResult()
        {
            // Arrange
            var filteredWord = "вечеринка";
            var ct = CancellationToken.None;
            var filter = new EventsFilter() { title = filteredWord };
            var now = DateTime.Now;
            var fakeEvents = new List<CreateEventDTO>
            {
                GetCreateEventDto(title: "Тестовое событие",startAt: DateTime.Now.AddHours(1),endAt:DateTime.Now.AddHours(2)),
                GetCreateEventDto(title: "Корпоратив",startAt: DateTime.Now.AddHours(2),endAt:DateTime.Now.AddHours(3)),
                GetCreateEventDto(title: "Ужин в ресторан",startAt: DateTime.Now.AddHours(2),endAt:DateTime.Now.AddHours(6)),
                GetCreateEventDto(title: "ВеЧеринкА на даче",startAt: DateTime.Now.AddHours(5),endAt:DateTime.Now.AddHours(12))
            };
            foreach (var @event in fakeEvents)
                await _eventService.CreateEventAsync(@event, ct);

            // Act
            var result = await _eventService.GetEventsAsync(filter, ct);

            // Assert
            Assert.NotNull(result);
            Assert.All(result.Events, q => Assert.Contains(filteredWord, q.Title, comparisonType: StringComparison.CurrentCultureIgnoreCase));
        }

        [Fact]
        [Trait("Category", "получение событий")]
        public async Task GetEventsAsync_WithFilterByStartDate_ReturnsPaginatedResult()
        {
            // Arrange
            var now = DateTime.Now;
            var filter = new EventsFilter() { from = now.AddHours(2) };
            var ct = CancellationToken.None;
            for (var i = 0; i < 3; i++)
            {
                var createEventDto = GetCreateEventDto(title: $"Тестовое событие {i}", startAt: now.AddHours(i + 1), endAt: now.AddHours(i + 2));
                await _eventService.CreateEventAsync(createEventDto, ct);
            }

            // Act
            var result = await _eventService.GetEventsAsync(filter, ct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalItems);
        }

        [Fact]
        [Trait("Category", "получение событий")]
        public async Task GetEventsAsync_WithFilterByEndDate_ReturnsPaginatedResult()
        {
            var now = DateTime.Now;
            var filter = new EventsFilter() { to = now.AddHours(3) };
            var ct = CancellationToken.None;
            for (var i = 0; i < 3; i++)
            {
                var createEventDto = GetCreateEventDto(title: $"Тестовое событие {i}", startAt: now.AddHours(i + 1), endAt: now.AddHours(i + 2));
                await _eventService.CreateEventAsync(createEventDto, ct);
            }

            // Act
            var result = await _eventService.GetEventsAsync(filter, ct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalItems);
        }

        [Fact]
        [Trait("Category", "получение событий")]
        public async Task GetEvents_GetSecondPage_ReturnsPaginatedResult()
        {
            // Arrange (Подготовка)

            var filter = new EventsFilter { page = 2, pageSize = 3 };
            var now = DateTime.Now;
            var ct = CancellationToken.None;
            // Создаем список тестовых данных
            for (var i = 0; i < 6; i++)
            {
                var createEventDto = GetCreateEventDto(title: $"Тестовое событие {i}", startAt: now.AddHours(i + 1), endAt: now.AddHours(i + 2));
                await _eventService.CreateEventAsync(createEventDto, ct);
            }

            // Act (Действие)
            var result = await _eventService.GetEventsAsync(filter, ct);

            // Assert (Проверка)
            Assert.NotNull(result);
            Assert.Equal(6, result.TotalItems); // Проверяем общее количество
            Assert.Equal(3, result.Events.Count); // Проверяем количество в текущей выборке
        }

        [Fact]
        [Trait("Category", "получение событий")]
        public async Task GetEventsAsync_WithManyFilter_ReturnsPaginatedResult()
        {
            // Arrange          
            var targetDate = DateTime.Now.AddHours(3);
            var now = DateTime.Now;
            var ct = CancellationToken.None;
            var filter = new EventsFilter
            {
                title = "Вечеринка",
                from = targetDate,
                page = 1,
                pageSize = 10
            };
            // Создаем список тестовых данных
            for (var i = 0; i < 6; i++)
            {
                var createEventDto = GetCreateEventDto(title: $"Тестовое событие {i}", startAt: now.AddHours(i + 1), endAt: now.AddHours(i + 2));
                await _eventService.CreateEventAsync(createEventDto, ct);
            }
            await _eventService.CreateEventAsync(GetCreateEventDto(title: "ВечЕринКА в 10 часов", startAt: targetDate, endAt: now.AddHours(9)), ct);

            // Act
            var result = await _eventService.GetEventsAsync(filter, ct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalItems);
            Assert.All(result.Events, q => Assert.Contains(filter.title, q.Title, StringComparison.InvariantCultureIgnoreCase));
            Assert.All(result.Events, q => Assert.True(q.StartAt >= filter.from));
        }


        [Fact]
        [Trait("Category", "получение событий")]
        public async Task GetEventsAsync_ReturnsThrowIfCancelled()
        {
            // Arrange
            var now = DateTime.Now;
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            // Act Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await _eventService.GetEventsAsync(new EventsFilter(), cts.Token));
        }

        #endregion

        #region  тесты: получение события

        [Fact]
        [Trait("Category", "получение события")]
        public async Task GetEventAsync_IfEventExist_ReturnResponseEventDTO()
        {
            // Arrange
            var now = DateTime.Now;
            var createEventDto = GetCreateEventDto(title: "Test Event", description: "Test Description", startAt: now, endAt: now.AddHours(2));
            var @event = await _eventService.CreateEventAsync(createEventDto, CancellationToken.None);

            // Act
            var result = await _eventService.GetEventAsync(@event.Id, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(@event.Title, result.Title);
            Assert.Equal(@event.StartAt, result.StartAt);
            Assert.Equal(@event.EndAt, result.EndAt);
        }

        [Fact]
        [Trait("Category", "получение события")]
        public async Task GetEventAsync_ThrowKeyNotExistException()
        {
            // Arrange
            var generatedId = Guid.NewGuid();
            var ct = CancellationToken.None;

            // Act Assert
            await Assert.ThrowsAsync<KeyNotExistException>(async () => await _eventService.GetEventAsync(generatedId, ct));
        }

        [Fact]
        [Trait("Category", "получение события")]
        public async Task GetEventAsync_ReturnThrowIfCancelled()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var now = DateTime.Now;
            var createEventDto = GetCreateEventDto(title: "Test Event", description: "Test Description", startAt: now, endAt: now.AddHours(2));
            var @event = await _eventService.CreateEventAsync(createEventDto, CancellationToken.None);
            // Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await _eventService.GetEventAsync(@event.Id, cts.Token));
        }

        #endregion

        #region  тесты: обновление события

        [Fact]
        [Trait("Category", "обновление события")]
        public async Task ChangeEventAsync_ReturnThrowKeyNotExistException()
        {
            // Arrange
            var now = DateTime.Now;
            var nonExistentId = Guid.NewGuid();
            var updateEventDto = GetUpdateEventDto(title: "Test Event", description: "Test Description", startAt: now, endAt: now.AddHours(2));
            var ct = CancellationToken.None;

            // Act Assert
            await Assert.ThrowsAsync<KeyNotExistException>(async () => await _eventService.ChangeEventAsync(nonExistentId, updateEventDto, CancellationToken.None));
        }

        [Fact]
        [Trait("Category", "обновление события")]
        public async Task ChangeEventAsync_WithDateFromMoreThanDateTo_ReturnThrowValidationException()
        {
            // Arrange
            var now = DateTime.Now;
            var createEventDto = GetCreateEventDto(title: "Test Event", description: "Test Description", startAt: now, endAt: now.AddHours(2));
            var @event = await _eventService.CreateEventAsync(createEventDto, CancellationToken.None);
            var ct = CancellationToken.None;

            // НЕВАЛИДНЫЕ данные: Начало  > Конец 
            var invalidUpdateDto = GetUpdateEventDto(title: "Test Event", description: "Test Description", startAt: now.AddDays(1), endAt: now.AddHours(2));

            // Assert
            await Assert.ThrowsAsync<ValidationException>(async () => await _eventService.ChangeEventAsync(@event.Id, invalidUpdateDto, ct));
        }


        [Fact]
        [Trait("Category", "обновление события")]
        public async Task ChangeEventAsync_WithTotalSeatsMoreThanRange_ReturnThrowValidationException()
        {
            // Arrange
            var now = DateTime.Now;
            var createEventDto = GetCreateEventDto(title: "Test Event", description: "Test Description", startAt: now, endAt: now.AddHours(2));
            var @event = await _eventService.CreateEventAsync(createEventDto, CancellationToken.None);
            var ct = CancellationToken.None;

            // НЕВАЛИДНЫЕ данные: TotalSeats < 1
            var invalidUpdateDto = GetUpdateEventDto(title: "Test Event", startAt: now.AddDays(1), endAt: now.AddHours(2), description: "Test Description", 0);
            // Assert
            await Assert.ThrowsAsync<ValidationException>(async () => await _eventService.ChangeEventAsync(@event.Id, invalidUpdateDto, ct));
        }

        [Fact]
        [Trait("Category", "обновление события")]
        public async Task ChangeEventAsync_ChangeAllData_Success()
        {
            // Arrange
            var now = DateTime.Now;
            var createEventDto = GetCreateEventDto(title: "Test Event", startAt: now, endAt: now.AddHours(2), description: "Test Description", 6);
            var @event = await _eventService.CreateEventAsync(createEventDto, CancellationToken.None);
            var ct = CancellationToken.None;

            var updateEventDTO = GetUpdateEventDto(title: "Test Event New!!!", startAt: now.AddHours(3), endAt: now.AddHours(6), description: "New Test Description", 3);

            // Act
            await _eventService.ChangeEventAsync(@event.Id, updateEventDTO, ct);
            var updatedEventDto = await _eventService.GetEventAsync(@event.Id, ct);

            // Assert
            Assert.Equal(updateEventDTO.Title, updatedEventDto!.Title);
            Assert.Equal(updateEventDTO.StartAt, updatedEventDto!.StartAt);
            Assert.Equal(updateEventDTO.EndAt, updatedEventDto!.EndAt);
            Assert.Equal(updateEventDTO.Description, updatedEventDto!.Description);
            Assert.Equal(updateEventDTO.TotalSeats, updatedEventDto!.TotalSeats);
        }


        [Fact]
        [Trait("Category", "обновление события")]
        public async Task ChangeEvent_ShouldThrowIfCancelled()
        {
            // Arrange
            var now = DateTime.Now;
            using var cts = new CancellationTokenSource();
            var createEventDto = GetCreateEventDto(title: "Test Event", startAt: now, endAt: now.AddHours(2), description: "Test Description", 6);
            var @event = await _eventService.CreateEventAsync(createEventDto, CancellationToken.None);
            cts.Cancel();
            var updateEventDTO = GetUpdateEventDto(title: "Test Event New!!!", startAt: now.AddHours(3), endAt: now.AddHours(6), description: "New Test Description", 3);

            //Act  Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await _eventService.ChangeEventAsync(@event.Id, updateEventDTO, cts.Token));
        }

        #endregion

        #region  ---тесты: удаление события

        [Fact]
        [Trait("Category", "удаление события")]
        public async Task RemoveEventAsync_Success()
        {
            // Arrange
            var ct = CancellationToken.None;
            var now = DateTime.UtcNow;
            var createEventDto = GetCreateEventDto(title: "Test Event", description: "Test Description", startAt: now, endAt: now.AddHours(2));
            var @event = await _eventService.CreateEventAsync(createEventDto, CancellationToken.None);
            //Act 
            await _eventService.RemoveEventAsync(@event.Id, ct);

            // Assert
            await Assert.ThrowsAsync<KeyNotExistException>(async () => await _eventService.GetEventAsync(@event.Id, ct));
        }

        [Fact]
        [Trait("Category", "удаление события")]
        public async Task RemoveEventAsync_ThrowNotFoundException()
        {
            // Arrange
            var ct = CancellationToken.None;
            var eventId = Guid.NewGuid();
            // Act Assert
            await Assert.ThrowsAsync<KeyNotExistException>(async () => await _eventService.RemoveEventAsync(eventId, ct));
        }

        [Fact]
        [Trait("Category", "удаление события")]
        public async Task RemoveEventAsync_ThrowIfCancelled()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // jотменяем токен
            var id = Guid.NewGuid();

            //Act Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await _eventService.RemoveEventAsync(id, cts.Token));
        }


        #endregion

        private CreateEventDTO GetCreateEventDto(string title, DateTime startAt, DateTime endAt, string? description = default, int totalSeats = 1)
        {
            return new CreateEventDTO() { Title = title, StartAt = startAt, EndAt = endAt, Description = description, TotalSeats = totalSeats };
        }

        private UpdateEventDTO GetUpdateEventDto(string title, DateTime startAt, DateTime endAt, string? description = default, int totalSeats = 1)
        {
            return new UpdateEventDTO() { Title = title, StartAt = startAt, EndAt = endAt, Description = description, TotalSeats = totalSeats };
        }
    }
}