using System.Linq.Expressions;
using EventsApi.Models.Domain;
using EventsApi.WebApi.Application.Services;
using EventsApi.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventsApi.UnitTests;

public class BookingBackgroundServiceTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<ILogger<BookingBackgroundService>> _loggerMock;

    public BookingBackgroundServiceTests()
    {
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _eventRepositoryMock = new Mock<IEventRepository>();
        _loggerMock = new Mock<ILogger<BookingBackgroundService>>();

        _serviceProviderMock = new Mock<IServiceProvider>();
        _scopeMock = new Mock<IServiceScope>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();

        SetupMocks();
    }

    private void SetupMocks()
    {
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IBookingRepository)))
            .Returns(_bookingRepositoryMock.Object);

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEventRepository)))
            .Returns(_eventRepositoryMock.Object);

        _scopeMock
            .Setup(s => s.ServiceProvider)
            .Returns(_serviceProviderMock.Object);

        _scopeFactoryMock
            .Setup(f => f.CreateScope())
            .Returns(_scopeMock.Object);

        _scopeFactoryMock
            .Setup(f => f.CreateScope())
            .Returns(_scopeMock.Object);
    }

    [Fact]
    public async Task ProcessBookingAsync_EventDeletedDuringProcessing_RejectsBooking()
    {
        // Arrange
        //var @event = 
        var booking = Booking.Create(Guid.NewGuid());
        var pendingBookings = new List<Booking> { booking };

        _bookingRepositoryMock
            .Setup(repo => repo.ListAsync(
                It.IsAny<Expression<Func<Booking, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingBookings);

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(booking.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var service = new BookingBackgroundService(
            _scopeFactoryMock.Object,
            _loggerMock.Object);

        // Act
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var serviceTask = service.StartAsync(cts.Token);

        await Task.Delay(3000, cts.Token);
        cts.Cancel();

        await Task.WhenAny(serviceTask, Task.Delay(5000, cts.Token));

        // Assert
        _eventRepositoryMock.Verify(
            repo => repo.GetByIdAsync(booking.EventId, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            "должна быть проверка существования события");

        _bookingRepositoryMock.Verify(
            repo => repo.UpdateAsync(
                It.Is<Booking>(b => b.Status == BookingStatus.Rejected),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            "бронь должна быть отклонена при отсутствии события");
    }

    [Fact]
    public async Task ProcessBookingAsync_ThrowDuringProcessing_RejectsBookingAndEventReleaseSeats()
    {
        // Arrange
        var @event = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 2);
        @event.AvailableSeats -= 1; //бронирем место
        var booking = Booking.Create(@event.Id);
        var pendingBookings = new List<Booking> { booking };

        _bookingRepositoryMock
            .Setup(repo => repo.ListAsync(
                It.IsAny<Expression<Func<Booking, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingBookings);
        _bookingRepositoryMock
                .SetupSequence(repo => repo.UpdateAsync(
                    It.IsAny<Booking>(),
                    It.IsAny<CancellationToken>()))
                .Throws(new InvalidOperationException())
                .Returns(Task.CompletedTask);

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(booking.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var service = new BookingBackgroundService(
            _scopeFactoryMock.Object,
            _loggerMock.Object);

        // Act
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var serviceTask = service.StartAsync(cts.Token);

        await Task.Delay(4000, cts.Token);
        cts.Cancel();

        await Task.WhenAny(serviceTask, Task.Delay(5000, cts.Token));

        // Assert

        Assert.Equal(2, @event.AvailableSeats);

        _eventRepositoryMock.Verify(
            repo => repo.GetByIdAsync(booking.EventId, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            "должна быть проверка существования события");

        _eventRepositoryMock.Verify(
             repo => repo.UpdateAsync(
                 It.Is<Event>(b => b.AvailableSeats == 2),
                 It.IsAny<CancellationToken>()),
             Times.AtLeastOnce,
             "должно быть восстановление мест при отклонении брони");

        _bookingRepositoryMock.Verify(
            repo => repo.UpdateAsync(
                It.Is<Booking>(b => b.Status == BookingStatus.Rejected),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            "бронь должна быть отклонена при отсутствии события");
    }
}