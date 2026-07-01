using EventsApi.Application.DTO.Booking;
using EventsApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EventsApi.Controllers;

[ApiController]
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
    [Tags("API для бронирования")]
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
}
