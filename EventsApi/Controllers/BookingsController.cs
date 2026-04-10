using EventsApi.Application.Interfaces;
using EventsApi.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    public async Task<IActionResult> GetBooking([Required] Guid bookingId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса GET {methodName}. Получение информации для бронирования: {bookingId}", nameof(GetBooking), bookingId);

        var bookingInfo = await bookingService.GetBookingByIdAsync(bookingId, ct);

        return Ok(bookingInfo);
    }
}
