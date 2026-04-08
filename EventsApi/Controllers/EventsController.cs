using System.ComponentModel.DataAnnotations;
using EventsApi.Application.Interfaces;
using EventsApi.Models.ModelDTO.Event;
using Microsoft.AspNetCore.Mvc;
namespace EventsApi.Controllers;
/// <summary>
/// Контроллер для работы с мероприятиями
/// </summary>
[Route("[controller]")]
[ApiController]
public class EventsController(IEventService eventService, ILogger<EventsController> logger) : ControllerBase
{

  /// <summary>
  /// Метод возвращает список мероприятий с пагинацией
  /// </summary>
  /// <param name="filter">фильтр значений</param>
  /// <param name="ct">токен отмены</param>
  [HttpGet]
  [Produces("application/json")]
  [ProducesResponseType(typeof(PaginatedResult), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetEventsAsync([FromQuery] EventsFilter filter, CancellationToken ct)
  {
    logger.LogDebug("Обработка запроса GET {methodName}", nameof(GetEventsAsync));
    return Ok(await eventService.GetEventsAsync(filter, ct));
  }

  /// <summary>
  /// Метод возвращает мероприятие по идентификатору из списка
  /// </summary>
  /// <param name="eventId">Параметр идентификатор мероприятия</param>
  /// <param name="ct">токен отмены</param>
  [HttpGet("{eventId:Guid}")]
  [Produces("application/json")]
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
  [Produces("application/json")]
  [ProducesResponseType(StatusCodes.Status201Created)]
  public async Task<IActionResult> AddEvent([FromBody][Required] InputEventDTO createEventDTO, CancellationToken ct)
  {
    logger.LogDebug("Обработка запроса POST {methodName}", nameof(AddEvent));
    var responseEventDto = await eventService.AddEventAsync(createEventDTO, ct);
    return CreatedAtAction(nameof(GetEventById), new { id = responseEventDto.Id }, responseEventDto);
  }

  /// <summary>
  /// Метод обновления даных мероприятия
  /// </summary>
  /// <param name="eventId">идентификатор мероприятия</param>
  /// <param name="updateEventDto">данне для обновления</param>
  /// <param name="ct">токен отмены</param>
  [HttpPut("{eventId:Guid}")]
  [Produces("application/json")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> UpdateEventAsync([Required] Guid eventId, [FromBody] InputEventDTO updateEventDto, CancellationToken ct)
  {
    logger.LogDebug("Обработка запроса PUT {methodName} c id: {id}", nameof(UpdateEventAsync), eventId);
    await eventService.ChangeEventAsync(eventId, updateEventDto, ct);
    return Ok();
  }

  /// <summary>
  /// Метод удаляет мероприятие по идентификатору из списка
  /// </summary>
  /// <param name="eventId">Параметр идентификатор мероприятия</param>
  /// <param name="ct">токен отмены</param>
  [HttpDelete("{eventId:Guid}")]
  [Produces("application/json")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> DeleteEventAsync([Required] Guid eventId, CancellationToken ct)
  {
    logger.LogDebug("Обработка запроса DELETE {methodName} c id: {id}", nameof(DeleteEventAsync), eventId);
    await eventService.RemoveEventAsync(eventId, ct);
    return Ok();
  }
}