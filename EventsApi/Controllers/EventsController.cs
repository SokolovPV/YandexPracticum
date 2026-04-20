using System.ComponentModel.DataAnnotations;
using EventsApi.Application.Interfaces;
using EventsApi.Models.ModelDTO.Booking;
using EventsApi.Models.ModelDTO.Event;
using Microsoft.AspNetCore.Mvc;
namespace EventsApi.Controllers;
/// <summary>
/// Контроллер для работы с мероприятиями
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class EventsController(IEventService eventService, IBookingService bookingService, ILogger<EventsController> logger) : ControllerBase
{

  /// <summary>
  /// Метод возвращает список мероприятий с пагинацией
  /// </summary>
  /// <param name="filter">фильтр значений</param>
  /// <param name="ct">токен отмены</param>
  [HttpGet]
  [Tags("АПИ для событий")]
  [ProducesResponseType(typeof(PaginatedResult), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetEvents([FromQuery] EventsFilter filter, CancellationToken ct)
  {
    logger.LogDebug("Обработка запроса GET {methodName}", nameof(GetEvents));
    return Ok(await eventService.GetEventsAsync(filter, ct));
  }

  /// <summary>
  /// Метод возвращает мероприятие по идентификатору из списка
  /// </summary>
  /// <param name="eventId">Параметр идентификатор мероприятия</param>
  /// <param name="ct">токен отмены</param>
  [HttpGet("{eventId:Guid}")]
  [Tags("АПИ для событий")]
  [ProducesResponseType(typeof(ResponseEventDTO), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> GetEventById([Required] Guid eventId, CancellationToken ct)
  {
    logger.LogDebug("Обработка запроса GET {methodName} по ID: {id} ", nameof(GetEventById), eventId);
    var responseEventDto = await eventService.GetEventAsync(eventId, ct);
    return responseEventDto is null ? NotFound() : Ok(responseEventDto);
  }

  /// <summary>
  /// Метод создает мероприятие
  /// </summary>
  /// <param name="createEventDTO">Новое мероприятие</param>
  /// <param name="ct">токен отмены</param>
  [HttpPost]
  [Tags("АПИ для событий")]
  [ProducesResponseType(StatusCodes.Status201Created)]
  public async Task<IActionResult> AddEvent([FromBody][Required] InputEventDTO createEventDTO, CancellationToken ct)
  {
    logger.LogDebug("Обработка запроса POST {methodName}", nameof(AddEvent));
    var responseEventDto = await eventService.AddEventAsync(createEventDTO, ct);
    return CreatedAtAction(nameof(GetEventById), new { eventId = responseEventDto.Id }, responseEventDto);
  }

  /// <summary>
  /// Метод обновления даных мероприятия
  /// </summary>
  /// <param name="eventId">идентификатор мероприятия</param>
  /// <param name="updateEventDto">данные для обновления</param>
  /// <param name="ct">токен отмены</param>
  [HttpPut("{eventId:Guid}")]
  [Tags("АПИ для событий")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> UpdateEvent([Required] Guid eventId, [FromBody] UpdateEventDTO updateEventDto, CancellationToken ct)
  {
    logger.LogDebug("Обработка запроса PUT {methodName} c id: {id}", nameof(UpdateEvent), eventId);
    await eventService.ChangeEventAsync(eventId, updateEventDto, ct);
    return Ok();
  }

  /// <summary>
  /// Метод удаляет мероприятие по идентификатору из списка
  /// </summary>
  /// <param name="eventId">Параметр идентификатор мероприятия</param>
  /// <param name="ct">токен отмены</param>
  [HttpDelete("{eventId:Guid}")]
  [Tags("АПИ для событий")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> DeleteEvent([Required] Guid eventId, CancellationToken ct)
  {
    logger.LogDebug("Обработка запроса DELETE {methodName} c id: {id}", nameof(DeleteEvent), eventId);
    await eventService.RemoveEventAsync(eventId, ct);
    return Ok();
  }

  /// <summary>
  /// Метод для создания бронирования
  /// </summary>
  [HttpPost("{eventId:guid}/book")]
  [Tags("АПИ для бронирования")]
  [ProducesResponseType(StatusCodes.Status202Accepted)]
  public async Task<IActionResult> AddBook([Required] Guid eventId, CancellationToken ct)
  {
    logger.LogDebug("Обработка запроса POST {methodName}", nameof(AddBook));
    var booking = await bookingService.CreateBookingAsync(eventId, ct);
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