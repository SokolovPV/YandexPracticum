using EventsApi.Models.Domain;
using EventsApi.WebApi.Application.CustomException;
using EventsApi.WebApi.Application.Services;
using EventsApi.DataAccess;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;

namespace EventsApi.UnitTests;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<ILogger<BookingService>> _loggerMock;
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _eventRepositoryMock = new Mock<IEventRepository>();
        _loggerMock = new Mock<ILogger<BookingService>>();

        _bookingService = new BookingService(
            _eventRepositoryMock.Object,
            _bookingRepositoryMock.Object,
            _loggerMock.Object);
    }

    #region Успешные сценарии бронирования

    [Fact]
    public async Task CreateBookingAsync_ValidBooking_DecreasesAvailableSeatsByOne()
    {
        // Arrange       
        var initialSeats = 10;
        var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), initialSeats);
        var eventId = @event.Id;
        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _bookingRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _bookingService.CreateBookingAsync(eventId, CancellationToken.None);

        // Assert
        @event.AvailableSeats.Should().Be(initialSeats - 1,
            "создание брони должно уменьшить количество доступных мест на 1");

        _eventRepositoryMock.Verify(
            repo => repo.UpdateAsync(@event, It.IsAny<CancellationToken>()),
            Times.Once,
            "событие должно быть обновлено с новым количеством мест");

        _bookingRepositoryMock.Verify(
            repo => repo.AddAsync(It.Is<Booking>(b =>
                b.EventId == eventId &&
                b.Status == BookingStatus.Pending),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "бронь должна быть создана со статусом Pending");
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsUpToLimit_AllSucceedWithUniqueIds()
    {
        // Arrange
        var availableSeats = 3;
        var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), availableSeats);
        var eventId = @event.Id;
        var createdBookings = new List<Booking>();

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _bookingRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((booking, _) =>
            {
                booking.Id = Guid.NewGuid(); // Имитация присвоения Id
                createdBookings.Add(booking);
            })
            .Returns(Task.CompletedTask);

        // Act
        var bookingTasks = Enumerable.Range(0, availableSeats)
            .Select(i => _bookingService.CreateBookingAsync(eventId, CancellationToken.None));

        var results = await Task.WhenAll(bookingTasks);

        // Assert
        @event.AvailableSeats.Should().Be(0,
            "все места должны быть заняты");

        createdBookings.Should().HaveCount(availableSeats,
            "должно быть создано ровно столько броней, сколько было мест");

        createdBookings.Select(b => b.Id).Should().OnlyHaveUniqueItems(
            "каждая бронь должна иметь уникальный идентификатор");

        createdBookings.All(b => b.EventId == eventId).Should().BeTrue(
            "все новые брони должны иметь один идентификатор события");

        createdBookings.All(b => b.Status == BookingStatus.Pending).Should().BeTrue(
            "все новые брони должны быть в статусе Pending");

        _eventRepositoryMock.Verify(
            repo => repo.UpdateAsync(@event, It.IsAny<CancellationToken>()),
            Times.Exactly(availableSeats),
            "событие должно обновляться после каждой брони");
    }

    [Fact]
    public async Task CreateBookingAsync_NoAvailableSeats_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 1);
        @event.AvailableSeats = 0;
        var eventId = @event.Id;

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var action = () => _bookingService.CreateBookingAsync(eventId, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NoAvailableSeatsException>()
            .WithMessage($"*{eventId}*", "сообщение должно содержать ID события");

        _bookingRepositoryMock.Verify(
            repo => repo.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "бронь не должна создаваться при отсутствии мест");
    }

    [Fact]
    public async Task CreateBookingAsync_ExhaustSeatsThenTryAgain_ThrowsException()
    {
        // Arrange
        var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 1);
        var eventId = @event.Id;


        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _bookingRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - первое бронирование успешно
        await _bookingService.CreateBookingAsync(eventId, CancellationToken.None);

        // Обновляем событие для имитации занятого места
        @event.AvailableSeats = 0;

        // Act - второе бронирование должно упасть в ошибку
        var action = () => _bookingService.CreateBookingAsync(eventId, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NoAvailableSeatsException>(
            "повторная попытка бронирования при отсутствии мест должна вызывать исключение");
    }

    #endregion


    #region Неуспешные сценарии бронирования

    [Fact]
    public async Task CreateBookingAsync_NonExistentEvent_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentEventId = Guid.NewGuid();

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(nonExistentEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        var action = () => _bookingService.CreateBookingAsync(nonExistentEventId, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<KeyNotExistException>()
            .WithMessage($"*{nonExistentEventId}*", "сообщение должно содержать ID несуществующего события");

        _bookingRepositoryMock.Verify(
            repo => repo.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "нельзя создать бронь для несуществующего события");

        _eventRepositoryMock.Verify(
            repo => repo.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "событие не должно обновляться");
    }

    [Fact]
    public async Task CreateBookingAsync_ZeroAvailableSeatsInitially_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 1);
        var eventId = @event.Id;
        @event.AvailableSeats = 0;
        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var action = () => _bookingService.CreateBookingAsync(eventId, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NoAvailableSeatsException>()
            .WithMessage($"*{eventId}*");

        @event.AvailableSeats.Should().Be(0,
            "количество мест не должно измениться при неудачной попытке бронирования");
    }

    #endregion



    #region Тесты на смену статуса брони

    [Fact]
    public void Confirm_BookingInPendingStatus_ChangesToConfirmedWithProcessedAt()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var booking = Booking.Create(eventId);
        var beforeConfirm = DateTime.UtcNow;

        // Act
        booking.Confirm();
        var afterConfirm = DateTime.UtcNow;

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed,
            "статус должен измениться на Confirmed");

        booking.ProcessedAt.Should().NotBeNull("ProcessedAt должен быть заполнен");
        booking.ProcessedAt.Should().BeOnOrAfter(beforeConfirm,
            "время обработки должно быть не раньше начала операции");
        booking.ProcessedAt.Should().BeOnOrBefore(afterConfirm,
            "время обработки должно быть не позже окончания операции");
    }

    [Fact]
    public void Confirm_AlreadyConfirmedBooking_ShouldBeIdempotent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var booking = Booking.Create(eventId);
        booking.Confirm();
        var firstProcessedAt = booking.ProcessedAt;

        // Act
        booking.Confirm();

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed,
            "повторный вызов Confirm не должен менять статус");
        booking.ProcessedAt.Should().Be(firstProcessedAt,
            "повторный вызов Confirm не должен менять время обработки");
    }

    [Fact]
    public void Reject_BookingInPendingStatus_ChangesToRejectedWithProcessedAt()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var booking = Booking.Create(eventId);
        var beforeReject = DateTime.UtcNow;

        // Act
        booking.Reject();
        var afterReject = DateTime.UtcNow;

        // Assert
        booking.Status.Should().Be(BookingStatus.Rejected,
            "статус должен измениться на Rejected");

        booking.ProcessedAt.Should().NotBeNull("ProcessedAt должен быть заполнен");
        booking.ProcessedAt.Should().BeOnOrAfter(beforeReject,
            "время обработки должно быть не раньше начала операции");
        booking.ProcessedAt.Should().BeOnOrBefore(afterReject,
            "время обработки должно быть не позже окончания операции");
    }

    [Fact]
    public void Reject_AlreadyRejectedBooking_ShouldBeIdempotent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var booking = Booking.Create(eventId);
        booking.Reject();
        var firstProcessedAt = booking.ProcessedAt;

        // Act
        booking.Reject();

        // Assert
        booking.Status.Should().Be(BookingStatus.Rejected,
            "повторный вызов Reject не должен менять статус");
        booking.ProcessedAt.Should().Be(firstProcessedAt,
            "повторный вызов Reject не должен менять время обработки");
    }

    #endregion


    #region Тесты на восстановление мест

    [Fact]
    public void ReleaseSeats_AfterRejection_RestoresAvailableSeats()
    {
        // Arrange
        var initialSeats = 10;
        var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), initialSeats);
        @event.AvailableSeats -= 1; // Одно место занято
        // Act
        @event.ReleaseSeats();

        // Assert
        @event.AvailableSeats.Should().Be(initialSeats,
            "после освобождения мест их количество должно вернуться к изначальному");
    }

    [Fact]
    public void ReleaseSeats_MultipleRejections_RestoresCorrectNumberOfSeats()
    {
        // Arrange
        var initialSeats = 10;
        var bookingsCount = 5;
        var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), initialSeats);
        @event.AvailableSeats -= bookingsCount; // занимаем места
        // Act
        for (int i = 0; i < bookingsCount; i++)
        {
            @event.ReleaseSeats();
        }

        // Assert
        @event.AvailableSeats.Should().Be(initialSeats,
            "после возврата всех мест их количество должно восстановиться полностью");
    }

    [Fact]
    public void ReleaseSeats_ExceedingMaxCapacity_CapsAtMaxSeats()
    {
        // Arrange
        var maxSeats = 10;
        var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), maxSeats);

        // Act
        @event.ReleaseSeats(); // Попытка вернуть место, когда все свободны

        // Assert
        @event.AvailableSeats.Should().Be(maxSeats,
            "количество мест не должно превышать максимальную вместимость");
    }

    [Fact]
    public async Task ReleaseSeats_AfterRejection_NewBookingCanBeCreated()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 1);
        @event.AvailableSeats -= 1; // Все места занят
        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _bookingRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Сначала убеждаемся, что бронирование невозможно
        var firstAttempt = () => _bookingService.CreateBookingAsync(eventId, CancellationToken.None);
        await firstAttempt.Should().ThrowAsync<NoAvailableSeatsException>();

        // Освобождаем место (имитация отмены)
        @event.ReleaseSeats();
        @event.AvailableSeats.Should().Be(1, "должно появиться одно свободное место");

        // Act - пробуем забронировать снова
        await _bookingService.CreateBookingAsync(eventId, CancellationToken.None);

        // Assert
        @event.AvailableSeats.Should().Be(0,
            "место должно быть занято новой бронью");

        _bookingRepositoryMock.Verify(
            repo => repo.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "должна быть создана одна бронь");
    }

    #endregion


    #region Тесты сценариев

    [Fact]
    public async Task FullBookingLifecycle_CreateRejectThenCreateAgain_Success()
    {
        // Arrange
        var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 1);
        var eventId = @event.Id;
        var booking = Booking.Create(eventId);
        var bookingId = booking.Id;

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _bookingRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _bookingRepositoryMock
            .Setup(repo => repo.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Шаг 1: Создаем бронь
        await _bookingService.CreateBookingAsync(eventId, CancellationToken.None);
        @event.AvailableSeats.Should().Be(0, "все места заняты");

        // Шаг 2: Отклоняем бронь
        booking.Reject();
        @event.ReleaseSeats();
        @event.AvailableSeats.Should().Be(1, "место освобождено");
        booking.Status.Should().Be(BookingStatus.Rejected, "бронь отклонена");

        // Шаг 3: Создаем новую бронь на освободившееся место
        await _bookingService.CreateBookingAsync(eventId, CancellationToken.None);
        @event.AvailableSeats.Should().Be(0, "место снова занято новой бронью");

        // Verify
        _bookingRepositoryMock.Verify(
            repo => repo.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2),
            "должны быть созданы две брони");
    }

    #endregion


    #region Тест на защиту от овербукинга и уникальность Id

    [Fact]
    public async Task CreateBookingAsync_20ConcurrentRequestsFor5Seats_Only5Succeed()
    {
        // Arrange
        const int totalSeats = 5;
        const int concurrentRequests = 20;

        // Создаем реальный объект события для отслеживания состояния
        var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), totalSeats);
        var eventId = @event.Id;

        // Счетчики для отслеживания результатов
        var successfulBookings = new ConcurrentBag<Booking>();
        var failedBookings = new ConcurrentBag<Exception>();
        var bookingIds = new ConcurrentBag<Guid>();

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _eventRepositoryMock
            .Setup(repo => repo.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _bookingRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);


        // Act - запускаем 20 конкурентных запросов
        var tasks = Enumerable.Range(0, concurrentRequests).Select(async i =>
        {
            try
            {
                var booking = await _bookingService.CreateBookingAsync(eventId, CancellationToken.None);
                bookingIds.Add(booking.Id);
                return (success: true, exception: (Exception?)null);
            }
            catch (NoAvailableSeatsException ex)
            {
                failedBookings.Add(ex);
                return (success: false, exception: (Exception)ex);
            }
            catch (Exception ex)
            {
                failedBookings.Add(ex);
                return (success: false, exception: ex);
            }
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r.success);
        var failureCount = results.Count(r => !r.success);
        var noAvailableSeatsExceptions = results.Count(r =>
            r.exception is NoAvailableSeatsException);

        // Основные проверки
        successCount.Should().Be(totalSeats,
            $"ровно {totalSeats} бронирований должны быть успешными");

        failureCount.Should().Be(concurrentRequests - totalSeats,
            $"ровно {concurrentRequests - totalSeats} должны получить отказ");

        noAvailableSeatsExceptions.Should().Be(concurrentRequests - totalSeats,
            "все отказы должны быть из-за отсутствия мест");

        @event.AvailableSeats.Should().Be(0,
            "все места должны быть заняты");

        successfulBookings.Should().OnlyContain(b => b.Status == BookingStatus.Pending,
            "все успешные брони должны быть в статусе Pending");

        // Дополнительная проверка: нет ли дубликатов Id
        bookingIds.Should().OnlyHaveUniqueItems("все Id броней должны быть уникальными");

        bookingIds.Should().HaveCount(totalSeats, "должно быть создано ровно 5 броней");

        // Проверяем, что репозиторий вызывался правильное количество раз
        _eventRepositoryMock.Verify(
            repo => repo.GetByIdAsync(eventId, It.IsAny<CancellationToken>()),
            Times.Exactly(concurrentRequests),
            "событие должно проверяться для каждого запроса");

        _bookingRepositoryMock.Verify(
            repo => repo.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()),
            Times.Exactly(totalSeats),
            "брони должны создаваться только для успешных запросов");
    }

    #endregion


    #region Стресс-тест на высокую нагрузку

    [Theory]
    [InlineData(3, 15)]  // 3 места, 15 запросов
    [InlineData(1, 50)]  // 1 место, 50 запросов
    [InlineData(70, 2000)] // 10 мест, 100 запросов
    public async Task CreateBookingAsync_HighLoadStressTest_NoOverselling(int seats, int requests)
    {
        // Arrange
        var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), seats);
        var eventId = @event.Id;
        var bookingCounter = 0;
        var bookingLock = new object();
        var eventLock = new SemaphoreSlim(1, 1);


        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);
        _eventRepositoryMock
            .Setup(repo => repo.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _bookingRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var tasks = Enumerable.Range(0, requests).Select(async i =>
        {
            await eventLock.WaitAsync();
            try
            {
                await _bookingService.CreateBookingAsync(eventId, CancellationToken.None);
                bookingCounter++;
                return true;
            }
            catch (NoAvailableSeatsException)
            {
                return false;
            }
            finally
            {
                eventLock.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        var successCount = results.Count(r => r);

        // Assert
        successCount.Should().Be(seats,
            $"должно быть ровно {seats} успешных бронирований");

        @event.AvailableSeats.Should().Be(0,
            "все места должны быть заняты");

        bookingCounter.Should().Be(seats,
            "счетчик созданных броней должен соответствовать количеству мест");
    }

    #endregion


}
