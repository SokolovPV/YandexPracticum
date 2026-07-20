
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using EventFlow.Entities.Redis;
using EventFlow.Events.Application.DTO;
using EventFlow.Events.Application.Interfaces;
using EventFlow.Events.Application.Options;
using EventFlow.Events.Application.Services;
using EventFlow.Events.Domain.Entities;
using EventFlow.Events.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EventFlow.Events.UnitTests
{
    public class EventServiceTests
    {
        private readonly Mock<IEventRepository> _repositoryMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IOptions<RedisOptions>> _redisOptionsMock;
        private readonly Mock<ILogger<EventService>> _loggerMock;
        private readonly EventService _eventService;
        private readonly RedisOptions _redisOptions;
        private readonly CancellationToken _cancellationToken;
        public EventServiceTests()
        {
            _repositoryMock = new Mock<IEventRepository>();
            _cacheMock = new Mock<ICacheService>();
            _loggerMock = new Mock<ILogger<EventService>>();

            _redisOptions = new RedisOptions
            {
                SingleExpirationTTL = 60,
                TopExpirationTTL = 120
            };

            _redisOptionsMock = new Mock<IOptions<RedisOptions>>();
            _redisOptionsMock.Setup(x => x.Value).Returns(_redisOptions);

            _eventService = new EventService(
                _repositoryMock.Object,
                _cacheMock.Object,
                _redisOptionsMock.Object,
                _loggerMock.Object
            );

            _cancellationToken = CancellationToken.None;
        }

        #region GetEventAsync Tests

        [Fact]
        public async Task GetEventAsync_ShouldReturnFromCache_WhenCacheHit()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var _event = Event.Create("Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1), 5);
            var eventDto = new EventInfoDTO(
                    Id: _event.Id,
                    Title: _event.Title,
                    Description: _event.Description,
                    StartAt: _event.StartAt,
                    EndAt: _event.EndAt,
                    TotalSeats: _event.TotalSeats,
                    AvailableSeats: _event.AvailableSeats);

            var cachedJson = JsonSerializer.Serialize(eventDto);

            _cacheMock
                .Setup(x => x.GetStringAsync(RedisKeys.ForEvent(eventId)))
                .ReturnsAsync(cachedJson);

            // Act
            var result = await _eventService.GetEventAsync(eventId, _cancellationToken);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(eventDto);

            // Проверяем, что репозиторий НЕ вызывался
            _repositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), _cancellationToken), Times.Never);
            _cacheMock.Verify(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task GetEventAsync_ShouldFetchFromRepositoryAndCache_WhenCacheMiss()
        {
            // Arrange
            var _event = Event.Create("Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1), 5);
            var eventDto = new EventInfoDTO(
                    Id: _event.Id,
                    Title: _event.Title,
                    Description: _event.Description,
                    StartAt: _event.StartAt,
                    EndAt: _event.EndAt,
                    TotalSeats: _event.TotalSeats,
                    AvailableSeats: _event.AvailableSeats);
            _cacheMock
                .Setup(x => x.GetStringAsync(RedisKeys.ForEvent(_event.Id)))
                .ReturnsAsync((string)null);

            _repositoryMock
                .Setup(x => x.GetByIdAsync(_event.Id, _cancellationToken))
                .ReturnsAsync(_event);

            // Act
            var result = await _eventService.GetEventAsync(_event.Id, _cancellationToken);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(eventDto);

            // Проверяем, что репозиторий вызван 1 раз
            _repositoryMock.Verify(x => x.GetByIdAsync(_event.Id, _cancellationToken), Times.Once);

            // Проверяем, что данные сохранены в кеш
            _cacheMock.Verify(
                x => x.SetStringAsync(
                    RedisKeys.ForEvent(_event.Id),
                    It.IsAny<string>(),
                    TimeSpan.FromMinutes(_redisOptions.SingleExpirationTTL)),
                Times.Once);
        }

        [Fact]
        public async Task GetEventAsync_ShouldThrowKeyNotExistException_WhenEventNotFound()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            _cacheMock
                .Setup(x => x.GetStringAsync(RedisKeys.ForEvent(eventId)))
                .ReturnsAsync((string)null);

            _repositoryMock
                .Setup(x => x.GetByIdAsync(eventId, _cancellationToken))
                .ReturnsAsync((Event)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotExistException>(() =>
                _eventService.GetEventAsync(eventId, _cancellationToken));

            _cacheMock.Verify(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        #endregion

        #region ChangeEventAsync Tests

        [Fact]
        public async Task ChangeEventAsync_ShouldUpdateEventAndInvalidateCache_WhenValidDataProvided()
        {
            // Arrange
            var _event = Event.Create("Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1), 5);
            var eventId = _event.Id;
            var updateDto = new UpdateEventDTO
            {
                Title = "Updated Title",
                Description = "Updated Description",
                StartAt = DateTime.UtcNow.AddDays(2),
                EndAt = DateTime.UtcNow.AddDays(3),
                TotalSeats = 80
            };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(eventId, _cancellationToken))
                .ReturnsAsync(_event);

            // Act
            await _eventService.ChangeEventAsync(eventId, updateDto, _cancellationToken);

            // Assert
            _repositoryMock.Verify(x => x.UpdateAsync(_event, _cancellationToken), Times.Once);

            // Проверяем, что кеш инвалидирован
            _cacheMock.Verify(x => x.KeyDeleteAsync(RedisKeys.ForEvent(eventId)), Times.Once);

            // Проверяем обновление свойств
            _event.Title.Should().Be(updateDto.Title);
            _event.Description.Should().Be(updateDto.Description);
            _event.StartAt.Should().Be(updateDto.StartAt.Value);
            _event.EndAt.Should().Be(updateDto.EndAt.Value);
            _event.TotalSeats.Should().Be(updateDto.TotalSeats.Value);
        }

        [Fact]
        public async Task ChangeEventAsync_ShouldNotUpdate_WhenUpdateDtoIsNull()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            // Act
            await _eventService.ChangeEventAsync(eventId, null, _cancellationToken);

            // Assert
            _repositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), _cancellationToken), Times.Never);
            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Event>(), _cancellationToken), Times.Never);
            _cacheMock.Verify(x => x.KeyDeleteAsync(It.IsAny<string>()), Times.Never);
        }


        [Fact]
        public async Task ChangeEventAsync_ShouldThrowKeyNotExistException_WhenEventNotFound()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var updateDto = new UpdateEventDTO { Title = "Updated Title" };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(eventId, _cancellationToken))
                .ReturnsAsync((Event)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotExistException>(() =>
                _eventService.ChangeEventAsync(eventId, updateDto, _cancellationToken));

            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Event>(), _cancellationToken), Times.Never);
            _cacheMock.Verify(x => x.KeyDeleteAsync(It.IsAny<string>()), Times.Never);
        }


        [Fact]
        public async Task ChangeEventAsync_ShouldThrowValidationException_WhenStartAtGreaterThanEndAt()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var updateDto = new UpdateEventDTO
            {
                StartAt = DateTime.UtcNow.AddDays(3),
                EndAt = DateTime.UtcNow.AddDays(2)
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                _eventService.ChangeEventAsync(eventId, updateDto, _cancellationToken));

            _repositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), _cancellationToken), Times.Never);
        }


        [Fact]
        public async Task ChangeEventAsync_ShouldThrowValidationException_WhenTotalSeatsOutOfRange()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var updateDto = new UpdateEventDTO { TotalSeats = 101 };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                _eventService.ChangeEventAsync(eventId, updateDto, _cancellationToken));
        }

        [Fact]
        public async Task ChangeEventAsync_ShouldThrowValidationException_WhenTotalSeatsLessThanBookedSeats()
        {
            // Arrange
            var _event = Event.Create("Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 80);
            var eventId = _event.Id;
            // Предположим, что 10 мест уже занято
            _event.TryReserveSeats(20); // Бронируем 20 мест

            var updateDto = new UpdateEventDTO { TotalSeats = 15 };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(eventId, _cancellationToken))
                .ReturnsAsync(_event);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                _eventService.ChangeEventAsync(eventId, updateDto, _cancellationToken));

            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Event>(), _cancellationToken), Times.Never);
        }

        [Fact]
        public async Task ChangeEventAsync_ShouldUpdateOnlyProvidedFields()
        {
            // Arrange
            var _event = Event.Create("Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1), 80, "Original Description");
            var eventId = _event.Id;
            var updateDto = new UpdateEventDTO { Title = "New Title Only" };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(eventId, _cancellationToken))
                .ReturnsAsync(_event);

            // Act
            await _eventService.ChangeEventAsync(eventId, updateDto, _cancellationToken);

            // Assert
            _event.Title.Should().Be("New Title Only");
            _event.Description.Should().Be("Original Description"); // Не изменилось
            _event.StartAt.Should().Be(_event.StartAt); // Не изменилось

            _repositoryMock.Verify(x => x.UpdateAsync(_event, _cancellationToken), Times.Once);
            _cacheMock.Verify(x => x.KeyDeleteAsync(RedisKeys.ForEvent(eventId)), Times.Once);
        }

        #endregion

        #region RemoveEventAsync Tests

        [Fact]
        public async Task RemoveEventAsync_ShouldDeleteEventAndInvalidateCache_WhenEventExists()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            _repositoryMock
                .Setup(x => x.DeleteAsync(eventId, _cancellationToken))
                .ReturnsAsync(true);

            // Act
            await _eventService.RemoveEventAsync(eventId, _cancellationToken);

            // Assert
            _repositoryMock.Verify(x => x.DeleteAsync(eventId, _cancellationToken), Times.Once);

            // Проверяем, что кеш инвалидирован
            _cacheMock.Verify(x => x.KeyDeleteAsync(RedisKeys.ForEvent(eventId)), Times.Once);
        }

        [Fact]
        public async Task RemoveEventAsync_ShouldThrowKeyNotExistException_WhenEventNotFound()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            _repositoryMock
                .Setup(x => x.DeleteAsync(eventId, _cancellationToken))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotExistException>(() =>
                _eventService.RemoveEventAsync(eventId, _cancellationToken));

            _cacheMock.Verify(x => x.KeyDeleteAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region TryReserveSeatAsync Tests

        [Fact]
        public async Task TryReserveSeatAsync_ShouldReserveSeat_WhenSeatsAvailable()
        {
            // Arrange           
            var eventEntity = Event.Create("Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
            var eventId = eventEntity.Id;
            var initialAvailableSeats = eventEntity.AvailableSeats;

            _repositoryMock
                .Setup(x => x.GetByIdAsync(eventId, _cancellationToken))
                .ReturnsAsync(eventEntity);

            // Act
            var result = await _eventService.TryReserveSeatAsync(eventId, _cancellationToken);

            // Assert
            result.Should().BeTrue();
            eventEntity.AvailableSeats.Should().Be(initialAvailableSeats - 1);
            _repositoryMock.Verify(x => x.UpdateAsync(eventEntity, _cancellationToken), Times.Once);
        }

        [Fact]
        public async Task TryReserveSeatAsync_ShouldReturnFalse_WhenNoSeatsAvailable()
        {
            // Arrange
            var eventEntity = Event.Create("Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 1);
            var eventId = eventEntity.Id;
            // Бронируем последнее место
            eventEntity.TryReserveSeats();

            _repositoryMock
                .Setup(x => x.GetByIdAsync(eventId, _cancellationToken))
                .ReturnsAsync(eventEntity);

            // Act
            var result = await _eventService.TryReserveSeatAsync(eventId, _cancellationToken);

            // Assert
            result.Should().BeFalse();
            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Event>(), _cancellationToken), Times.Never);
        }

        [Fact]
        public async Task TryReserveSeatAsync_ShouldThrowKeyNotExistException_WhenEventNotFound()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            _repositoryMock
                .Setup(x => x.GetByIdAsync(eventId, _cancellationToken))
                .ReturnsAsync((Event)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotExistException>(() =>
                _eventService.TryReserveSeatAsync(eventId, _cancellationToken));
        }

        [Fact]
        public async Task TryReserveSeatAsync_ShouldThrowEventAlreadyStartedException_WhenEventStarted()
        {
            // Arrange
            var eventEntity = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(2), 10);
            var eventId = eventEntity.Id;

            await Task.Delay(1000,_cancellationToken);
            _repositoryMock
                .Setup(x => x.GetByIdAsync(eventId, _cancellationToken))
                .ReturnsAsync(eventEntity);

            // Act & Assert
            await Assert.ThrowsAsync<EventAlreadyStartedException>(() =>
                _eventService.TryReserveSeatAsync(eventId, _cancellationToken));

            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Event>(), _cancellationToken), Times.Never);
        }

        #endregion

        #region ReleaseSeatAsync Tests

        [Fact]
        public async Task ReleaseSeatAsync_ShouldReleaseSeatAndInvalidateCache_WhenEventExists()
        {
            // Arrange
            var eventEntity = Event.Create("Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
            var eventId = eventEntity.Id;
            eventEntity.TryReserveSeats(); // Бронируем место

            _repositoryMock
                .Setup(x => x.GetByIdAsync(eventId, _cancellationToken))
                .ReturnsAsync(eventEntity);

            // Act
            var result = await _eventService.ReleaseSeatAsync(eventId, _cancellationToken);

            // Assert
            result.Should().BeTrue();
            _repositoryMock.Verify(x => x.UpdateAsync(eventEntity, _cancellationToken), Times.Once);

            // Проверяем инвалидацию кеша
            _cacheMock.Verify(x => x.KeyDeleteAsync(RedisKeys.ForEvent(eventId)), Times.Once);
            _cacheMock.Verify(x => x.KeyDeleteAsync(RedisKeys.TopEvents), Times.Once);
        }

        [Fact]
        public async Task ReleaseSeatAsync_ShouldThrowKeyNotExistException_WhenEventNotFound()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            _repositoryMock
                .Setup(x => x.GetByIdAsync(eventId, _cancellationToken))
                .ReturnsAsync((Event)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotExistException>(() =>
                _eventService.ReleaseSeatAsync(eventId, _cancellationToken));
        }

        #endregion

        #region GetTop10EventsAsync Tests

        [Fact]
        public async Task GetTop10EventsAsync_ShouldReturnFromCache_WhenCacheHit()
        {
            // Arrange
            var expectedEvents = CreateTop10Events();
            var expectedResult = new PaginatedResultTop10
            (
                Events : expectedEvents.Select(MapToEventInfoDTO).ToList()
            );
            var cachedJson = JsonSerializer.Serialize(expectedResult);

            _cacheMock
                .Setup(x => x.GetStringAsync(RedisKeys.TopEvents))
                .ReturnsAsync(cachedJson);

            // Act
            var result = await _eventService.GetTop10EventsAsync(_cancellationToken);

            // Assert
            result.Should().NotBeNull();
            result.Events.Should().HaveCount(10);

            _repositoryMock.Verify(x => x.GetTop10EventAsync(_cancellationToken), Times.Never);
            _cacheMock.Verify(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task GetTop10EventsAsync_ShouldFetchFromRepositoryAndCache_WhenCacheMiss()
        {
            // Arrange
            var expectedEvents = CreateTop10Events();

            _cacheMock
                .Setup(x => x.GetStringAsync(RedisKeys.TopEvents))
                .ReturnsAsync((string)null);

            _repositoryMock
                .Setup(x => x.GetTop10EventAsync(_cancellationToken))
                .ReturnsAsync(expectedEvents);

            // Act
            var result = await _eventService.GetTop10EventsAsync(_cancellationToken);

            // Assert
            result.Should().NotBeNull();
            result.Events.Should().HaveCount(10);

            _repositoryMock.Verify(x => x.GetTop10EventAsync(_cancellationToken), Times.Once);

            _cacheMock.Verify(
                x => x.SetStringAsync(
                    RedisKeys.TopEvents,
                    It.IsAny<string>(),
                    TimeSpan.FromMinutes(_redisOptions.TopExpirationTTL)),
                Times.Once);
        }

        [Fact]
        public async Task GetTop10EventsAsync_ShouldReturnEmptyResult_WhenNoEvents()
        {
            // Arrange
            _cacheMock
                .Setup(x => x.GetStringAsync(RedisKeys.TopEvents))
                .ReturnsAsync((string)null);

            _repositoryMock
                .Setup(x => x.GetTop10EventAsync(_cancellationToken))
                .ReturnsAsync(new List<Event>());

            // Act
            var result = await _eventService.GetTop10EventsAsync(_cancellationToken);

            // Assert
            result.Should().NotBeNull();
            result.Events.Should().BeEmpty();
        }

        #endregion

        #region Helper Methods

        private List<Event> CreateTop10Events()
        {
            var events = new List<Event>();
            for (int i = 1; i <= 10; i++)
            {
                events.Add(CreateTestEvent(
                    Guid.NewGuid(),
                    $"Top Event {i}",
                    $"Description {i}",
                    totalSeats: 100 - i * 5
                ));
            }
            return events;
        }
        private EventInfoDTO MapToEventInfoDTO(Event eventEntity)
        {
            return new EventInfoDTO(
                Id: eventEntity.Id,
                Title: eventEntity.Title,
                Description: eventEntity.Description,
                StartAt: eventEntity.StartAt,
                EndAt: eventEntity.EndAt,
                TotalSeats: eventEntity.TotalSeats,
                AvailableSeats: eventEntity.AvailableSeats
            );
        }

        private Event CreateTestEvent(
            Guid id,
            string title = "Test Event",
            string description = "Test Description",
            DateTime? startAt = null,
            DateTime? endAt = null,
            int totalSeats = 50)
        {
            return Event.Create(
                title: title,
                description: description,
                startAt: startAt ?? DateTime.UtcNow.AddDays(1),
                endAt: endAt ?? DateTime.UtcNow.AddDays(2),
                totalSeats: totalSeats
            );
        }
        #endregion

    }
}
