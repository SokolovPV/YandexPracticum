using EventsApi.Application.DTO.Booking;
using EventsApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

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
}
