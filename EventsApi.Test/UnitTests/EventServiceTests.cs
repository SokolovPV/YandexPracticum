using System.ComponentModel.DataAnnotations;
using EventsApi.DataAccess;
using EventsApi.Models.Domain;
using EventsApi.Models.ModelDTO.Event;
using EventsApi.WebApi.Application.CustomException;
using EventsApi.WebApi.Application.Services;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Moq;

namespace EventsApi.Test
{
    public class EventServiceTests
    {

        #region тесты: создание события

        [Fact]
        [Trait("Category", " создание события")]
        public async Task CreateEvent_ReturnResponseEventDTO_Success()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var ct = CancellationToken.None;
            int count_seats = 10;
            var dto = new CreateEventDTO
            {
                Title = "Тестовое событие",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = count_seats
            };
            repositoryMock.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()));

            // Act
            var result = await service.CreateEventAsync(dto, ct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Title, result.Title);
            Assert.Equal(dto.EndAt, result.EndAt);
            Assert.Equal(dto.StartAt, result.StartAt);
            Assert.Equal(count_seats, result.TotalSeats);
            Assert.Equal(count_seats, result.AvailableSeats);
            Assert.NotEqual(Guid.Empty, result.Id);
            repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", " создание события")]
        public async Task CreateEvent_DateFromMoreThanDateTo_ThrowValidationException()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var ct = CancellationToken.None;
            var dto = new CreateEventDTO
            {
                Title = "Тестовое событие с невалидной моделью данных",
                StartAt = DateTime.Now.AddHours(2),
                EndAt = DateTime.Now.AddHours(1), // Конец позже начала
                TotalSeats = 1
            };
            repositoryMock.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()));

            //Act  Assert
            await Assert.ThrowsAsync<ValidationException>(async () => await service.CreateEventAsync(dto, ct));
            repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>(), ct), Times.Never);
        }

        [Fact]
        [Trait("Category", " создание события")]
        public async Task CreateEvent_TotalSeatsMoreThanRange_ThrowValidationException()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var ct = CancellationToken.None;
            var dto = new CreateEventDTO
            {
                Title = "Тестовое событие с невалидной моделью данных",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2),
                TotalSeats = 200 // больше 100
            };
            repositoryMock.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()));

            //Act  Assert
            await Assert.ThrowsAsync<ValidationException>(async () => await service.CreateEventAsync(dto, ct));
            repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>(), ct), Times.Never);
        }


        [Fact]
        [Trait("Category", " создание события")]
        public async Task CreateEvent_TotalSeatsLessThanRange_ThrowValidationException()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var ct = CancellationToken.None;
            var dto = new CreateEventDTO
            {
                Title = "Тестовое событие с невалидной моделью данных",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2),
                TotalSeats = 0 // меньше 1
            };
            repositoryMock.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()));

            //Act  Assert
            await Assert.ThrowsAsync<ValidationException>(async () => await service.CreateEventAsync(dto, ct));
            repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>(), ct), Times.Never);
        }


        [Fact]
        [Trait("Category", " создание события")]
        public async Task CreateEvent_ThrowOperationCanceledException()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var dto = new CreateEventDTO
            {
                Title = "Тестовое событие",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2),
                TotalSeats = 1
            };
            repositoryMock.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()));

            // Act Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await service.CreateEventAsync(dto, cts.Token));
            repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion


        #region тесты: получение событий

        [Fact]
        [Trait("Category", "получение событий")]
        public async Task GetEvents_ReturnPaginatedResult()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var filter = new EventsFilter();
            var now = DateTime.Now;
            var ct = CancellationToken.None;
            var fakeEvents = new List<Event>
            {
                Event.Create("Корпоратив", now.AddHours(1),now.AddHours(2), 1),
                Event.Create("Ужин в ресторане", now.AddHours(2), now.AddHours(3), 1),
                Event.Create("Вечеринка ", now.AddHours(3),now.AddHours(4), 1)
            };
            repositoryMock
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeEvents.Count);
            repositoryMock
                .Setup(r => r.ListAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeEvents);

            // Act
            var result = await service.GetEventsAsync(filter, ct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(fakeEvents.Count, result.TotalItems);
            Assert.Collection(result.Events, p =>
            {
                Assert.Equal(fakeEvents[0].Title, p.Title);
                Assert.Equal(fakeEvents[0].StartAt, p.StartAt);
                Assert.Equal(fakeEvents[0].EndAt, p.EndAt);
            },
            p =>
            {
                Assert.Equal(fakeEvents[1].Title, p.Title);
                Assert.Equal(fakeEvents[1].StartAt, p.StartAt);
                Assert.Equal(fakeEvents[1].EndAt, p.EndAt);
            },
            p =>
            {
                Assert.Equal(fakeEvents[2].Title, p.Title);
                Assert.Equal(fakeEvents[2].StartAt, p.StartAt);
                Assert.Equal(fakeEvents[2].EndAt, p.EndAt);
            });

            repositoryMock.Verify(r => r.ListAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "получение событий")]
        public async Task GetEvents_ReturnPaginatedResult_FilteredByName()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var filteredWord = "вечеринка";
            var ct = CancellationToken.None;
            var filter = new EventsFilter() { title = filteredWord };
            var now = DateTime.Now;
            var fakeEvents = new List<Event>
            {
                Event.Create("Корпоратив", now.AddHours(1),now.AddHours(2), 1),
                Event.Create("Ужин в ресторане", now.AddHours(2), now.AddHours(3), 1),
                Event.Create("ВеЧеринкА на высшем уровне", now.AddHours(3),now.AddHours(4), 1)
            };
            repositoryMock
                .Setup(r => r.ListAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeEvents.Where(q => q.Title.Contains(filteredWord, StringComparison.CurrentCultureIgnoreCase)).ToList());

            // Act
            var result = await service.GetEventsAsync(filter, ct);

            // Assert
            Assert.NotNull(result);
            Assert.All(result.Events, q => Assert.Contains(filteredWord, q.Title, comparisonType: StringComparison.CurrentCultureIgnoreCase));

            repositoryMock.Verify(r => r.ListAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "получение событий")]
        public async Task GetEvents_ReturnPaginatedResult_FilteredByStartDate()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var now = DateTime.Now;
            var filter = new EventsFilter() { from = now.AddHours(2) };
            var ct = CancellationToken.None;
            var fakeEvents = new List<Event>
            {
                Event.Create("Корпоратив", now.AddHours(1),now.AddHours(2), 1),
                Event.Create("Ужин в ресторане", now.AddHours(2), now.AddHours(3), 1),
                Event.Create("Вечеринка на высшем уровне", now.AddHours(3),now.AddHours(4), 1)
            };
            repositoryMock
                .Setup(r => r.ListAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeEvents.Where(q => q.StartAt >= filter.from).ToList());
            repositoryMock
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeEvents.Count(q => q.StartAt >= filter.from));
            // Act
            var result = await service.GetEventsAsync(filter, ct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalItems);

            repositoryMock.Verify(r => r.ListAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "получение событий")]
        public async Task GetEvents_ReturnPaginatedResult_FilteredByEndDate()
        {
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var now = DateTime.Now;
            var filter = new EventsFilter() { to = now.AddHours(3) };
            var ct = CancellationToken.None;
            var fakeEvents = new List<Event>
            {
                Event.Create("Корпоратив", now.AddHours(1),now.AddHours(2), 1),
                Event.Create("Ужин в ресторане", now.AddHours(2), now.AddHours(3), 1),
                Event.Create("Вечеринка на высшем уровне", now.AddHours(3),now.AddHours(4), 1)
            };

            repositoryMock
                .Setup(r => r.ListAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeEvents.Where(q => q.EndAt <= filter.to).ToList());
            repositoryMock
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeEvents.Count(q => q.EndAt <= filter.to));

            // Act
            var result = await service.GetEventsAsync(filter, ct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalItems);
            repositoryMock.Verify(r => r.ListAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "получение событий")]
        public async Task GetEvents_ReturnPaginatedResult_SecondPage()
        {
            // Arrange (Подготовка)
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var filter = new EventsFilter { page = 2, pageSize = 3 };
            var now = DateTime.Now;
            var ct = CancellationToken.None;
            // Создаем список тестовых данных, которые "якобы" есть в репозитории
            var fakeEvents = new List<Event>
            {
                Event.Create("Событие 1", now, now.AddHours(1), 1),
                Event.Create("Событие 2", now, now.AddHours(1), 1),
                Event.Create("Событие 3", now, now.AddHours(1), 1),
                Event.Create("Событие 4", now, now.AddHours(1), 1),
                Event.Create("Событие 5", now, now.AddHours(1), 1),
                Event.Create("Событие 6", now, now.AddHours(1), 1)
            };

            repositoryMock
                .Setup(r => r.ListAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeEvents.OrderBy(q => q.Title).Skip((filter.page - 1) * filter.pageSize).Take(filter.pageSize).ToList());
            repositoryMock
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeEvents.Count);

            // Act (Действие)
            var result = await service.GetEventsAsync(filter, ct);

            // Assert (Проверка)
            Assert.NotNull(result);
            Assert.Equal(6, result.TotalItems); // Проверяем общее количество
            Assert.Equal(3, result.Events.Count); // Проверяем количество в текущей выборке

            repositoryMock.Verify(r => r.ListAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "получение событий")]
        public async Task GetEvents_ReturnPaginatedResult_WithManyFilters()
        {
            // Arrange (Подготовка)
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
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
            var fakeEvents = new List<Event>
            {
                Event.Create("Событие 1", targetDate, now.AddHours(5), 1),
                Event.Create("Событие 2", now.AddHours(1), now.AddHours(5), 1),
                Event.Create("Событие 3", now, now.AddHours(5), 1),
                Event.Create("Вечеринка в 10 часов", targetDate, now.AddHours(5), 1),
                Event.Create("Событие 4", now.AddHours(2), now.AddHours(5), 1),
                Event.Create("Событие 5", now.AddHours(1), now.AddHours(5), 1)
            };

            Func<Event, bool> queru = q => q.Title.Contains(filter.title, StringComparison.InvariantCultureIgnoreCase) && q.StartAt >= filter.from;

            // Настраиваем Mock репозиторий возвращать список тестовых данных
            repositoryMock
                .Setup(r => r.ListAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeEvents.Where(queru).ToList());
            repositoryMock
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeEvents.Count(queru));

            // Act (Действие)
            var result = await service.GetEventsAsync(filter, ct);

            // Assert (Проверка)
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalItems);
            Assert.All(result.Events, q => Assert.Contains(filter.title, q.Title, StringComparison.InvariantCultureIgnoreCase));
            Assert.All(result.Events, q => Assert.True(q.StartAt >= filter.from));


            repositoryMock.Verify(r => r.ListAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        [Trait("Category", "получение событий")]
        public async Task GetEvents_ThrowIfCancelled()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var fakeEvents = new List<Event>
            {
                Event.Create("Событие", DateTime.Now.AddHours(1), DateTime.Now.AddHours(2), 1),
            };
            var filter = new EventsFilter { page = 2, pageSize = 2 };
            repositoryMock
                .Setup(r => r.ListAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeEvents);
            repositoryMock
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeEvents.Count());

            // Act Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await service.GetEventsAsync(filter, cts.Token));
            repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region  тесты: получение события

        [Fact]
        [Trait("Category", "получение события")]
        public async Task GetEvent_ReturnResponseEventDTO_IfEventExist()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var domainEvent = Event.Create("Тестовое событие", DateTime.Now, DateTime.Now.AddHours(5), 1);
            var generatedId = domainEvent.Id;
            var ct = CancellationToken.None;
            repositoryMock
                .Setup(r => r.GetByIdAsync(generatedId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(domainEvent);

            // Act
            var result = await service.GetEventAsync(generatedId, ct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(domainEvent.Title, result.Title);
            Assert.Equal(domainEvent.StartAt, result.StartAt);
            Assert.Equal(domainEvent.EndAt, result.EndAt);

            repositoryMock.Verify(r => r.GetByIdAsync(generatedId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "получение события")]
        public async Task GetEvent_ThrowKeyNotExistException()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var generatedId = Guid.NewGuid();
            var ct = CancellationToken.None;

            repositoryMock.Setup(r => r.GetByIdAsync(generatedId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

            // Act Assert
            await Assert.ThrowsAsync<KeyNotExistException>(async () => await service.GetEventAsync(generatedId, ct));

            repositoryMock.Verify(r => r.GetByIdAsync(generatedId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "получение события")]
        public async Task GetEvent_ThrowIfCancelled()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(It.IsAny<Event>());

            // Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await service.GetEventAsync(It.IsAny<Guid>(), cts.Token));
            repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region  тесты: обновление события

        [Fact]
        [Trait("Category", "обновление события")]
        public async Task ChangeEvent_ThrowKeyNotExistException()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var nonExistentId = Guid.NewGuid();
            var ct = CancellationToken.None;

            var updateDto = new UpdateEventDTO
            {
                Title = "Тестовое событие",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 1
            };
            repositoryMock.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

            // Act Assert
            await Assert.ThrowsAsync<KeyNotExistException>(async () => await service.ChangeEventAsync(nonExistentId, updateDto, ct));

            // Проверяем, что метод Update у репозитория не вызывался
            repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [Trait("Category", "обновление события")]
        public async Task ChangeEvent_DateFromMoreThanDateTo_ThrowValidationException()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var now = DateTime.Now;
            var existedEvent = Event.Create("Old событие", now, now.AddHours(1), 1);
            var eventId = existedEvent.Id;
            var ct = CancellationToken.None;

            // НЕВАЛИДНЫЕ данные: Начало (5ч) > Конец (2ч)
            var invalidUpdateDto = new UpdateEventDTO
            {
                Title = "Обновление",
                StartAt = now.AddHours(5),
                EndAt = now.AddHours(2),
                TotalSeats = 1
            };

            repositoryMock.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(existedEvent);

            // Assert
            await Assert.ThrowsAsync<ValidationException>(async () => await service.ChangeEventAsync(eventId, invalidUpdateDto, ct));

            repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
        }


        [Fact]
        [Trait("Category", "обновление события")]
        public async Task ChangeEvent_TotalSeatsMoreThanRange_ThrowValidationException()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var now = DateTime.Now;
            var existedEvent = Event.Create("Old событие", now, now.AddHours(1), 1);
            var eventId = existedEvent.Id;
            var ct = CancellationToken.None;

            // НЕВАЛИДНЫЕ данные: TotalSeats < 1

            var invalidUpdateDto = new UpdateEventDTO
            {
                Title = "Обновление",
                StartAt = now.AddHours(1),
                EndAt = now.AddHours(2),
                TotalSeats = 0
            };

            repositoryMock.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(existedEvent);

            // Assert
            await Assert.ThrowsAsync<ValidationException>(async () => await service.ChangeEventAsync(eventId, invalidUpdateDto, ct));

            repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [Trait("Category", "обновление события")]
        public async Task ChangeEvent_ChangeAllData_Success()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var now = DateTime.Now;
            var ct = CancellationToken.None;

            var existedEvent = Event.Create("old title", now, now.AddHours(1), 1);
            var eventId = existedEvent.Id;

            var updateDto = new UpdateEventDTO
            {
                Title = "New title",
                StartAt = now.AddDays(1),
                EndAt = now.AddDays(1).AddHours(2),
                TotalSeats = 1,
                Description = "New Description"
            };

            repositoryMock
                .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existedEvent);

            // Act
            await service.ChangeEventAsync(eventId, updateDto, ct);
            var updatetEventDto = await service.GetEventAsync(eventId, ct);

            // Assert
            Assert.Equal(updateDto.Title, updatetEventDto!.Title);
            Assert.Equal(updateDto.StartAt, updatetEventDto!.StartAt);
            Assert.Equal(updateDto.EndAt, updatetEventDto!.EndAt);
            Assert.Equal(updateDto.Description, updatetEventDto!.Description);

            // Проверяем, что сервис вызвал Update у репозитория один раз с этим объектом
            repositoryMock.Verify(r => r.UpdateAsync(existedEvent, It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        [Trait("Category", "обновление события")]
        public async Task ChangeEvent_ShouldThrowIfCancelled()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            repositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(It.IsAny<Event>());

            //Act  Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await service.ChangeEventAsync(It.IsAny<Guid>(), It.IsAny<UpdateEventDTO>(), cts.Token));
            repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region  тесты: удаление события

        [Fact]
        [Trait("Category", "удаление события")]
        public async Task DeleteEvent_Success()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var ct = CancellationToken.None;

            repositoryMock
                .Setup(r => r.DeleteAsync(eventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await service.RemoveEventAsync(eventId, ct);

            // Assert
            repositoryMock.Verify(r => r.DeleteAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "удаление события")]
        public async Task DeleteEvent_ThrowNotFoundException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var ct = CancellationToken.None;

            repositoryMock.Setup(r => r.DeleteAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            // Act Assert
            await Assert.ThrowsAsync<KeyNotExistException>(async () => await service.RemoveEventAsync(eventId, ct));
            repositoryMock.Verify(r => r.DeleteAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "удаление события")]
        public async Task DeleteEvent_ThrowIfCancelled()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // jотменяем токен
            var id = Guid.NewGuid();
            repositoryMock.Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            //Act Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await service.RemoveEventAsync(id, cts.Token));
            repositoryMock.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Never);
        }


        #endregion
    }
}