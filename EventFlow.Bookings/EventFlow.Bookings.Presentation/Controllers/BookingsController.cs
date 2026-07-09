using EventFlow.Bookings.Application.DTO;
using EventFlow.Bookings.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace EventFlow.Bookings.Presentation.Controllers;

[ApiController]
[Authorize(Policy = "CustomJwtPolicy")]
[Route("[controller]")]
[Produces("application/json")]
public class BookingsController(IBookingService bookingService, ILogger<BookingsController> logger) : ControllerBase
{
    /// <summary>
    /// Информация по бронированию
    /// </summary>
    [HttpGet("{bookingId:guid}")]
    [Tags("АПИ для бронирования")]
    [ProducesResponseType(typeof(InfoBookingDTO), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBooking([Required] Guid bookingId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса GET {methodName}. Получение информации по бронированию: {bookingId}", nameof(GetBooking), bookingId);

        var booking = await bookingService.GetBookingByIdAsync(bookingId, ct);
        var infoBookingDTO = new InfoBookingDTO(
            Id: booking.Id,
            EventID: booking.EventId,
            UserID: booking.UserId,
            Status: booking.Status.ToString(),
            CreatedAt: booking.CreatedAt,
            ProcessedAt: booking.ProcessedAt
        );

        return Ok(infoBookingDTO);
    }

    /// <summary>
    /// Удаление бронирования
    /// </summary>
    [HttpDelete("{bookingId:guid}")]
    [Tags("АПИ для бронирования")]
    public async Task<IActionResult> CancelBooking([Required] Guid bookingId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса DELETE {methodName}. Удаление бронирования: {bookingId}", nameof(CancelBooking), bookingId);

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim?.Value) || !Guid.TryParse(userIdClaim.Value, out Guid userId))
        {
            return Unauthorized("Не удалось определить идентификатор пользователя.");
        }

        await bookingService.CancelBookingAsync(bookingId, userId, ct);
        return NoContent();
    }

    /// <summary>
    /// Метод для создания бронирования
    /// </summary>
    [HttpPost("{eventId:guid}")]
    [Tags("АПИ для бронирования")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddBooking([Required] Guid eventId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса POST {methodName}", nameof(AddBooking));
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim?.Value) || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Идентификатор пользователя не верен.");

        var booking = await bookingService.CreateBookingAsync(eventId, userId, ct);
        var responseDto = new CreatedBookingDTO
        {
            Id = booking.Id,
            Status = booking.Status.ToString(),
            CreatedAt = booking.CreatedAt,
            EventID = booking.EventId
        };

        return AcceptedAtAction(
            actionName: "GetBooking",
            controllerName: "Bookings",
            routeValues: new { bookingId = booking.Id },
            value: responseDto
        );
    }
}
