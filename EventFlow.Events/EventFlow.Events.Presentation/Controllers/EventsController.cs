using EventFlow.Entities.Constant;
using EventFlow.Entities.Enums;
using EventFlow.Events.Application.DTO;
using EventFlow.Events.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EventFlow.Events.Presentation.Controllers;
/// <summary>
/// Контроллер для работы с мероприятиями
/// </summary>
[Authorize(Policy = StringConstant.JwtPolicyName)]
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class EventsController(IEventService eventService, ILogger<EventsController> logger) : ControllerBase
{

  /// <summary>
  /// Метод возвращает список мероприятий с пагинацией
  /// </summary>
  /// <param name="filter">фильтр значений</param>
  /// <param name="ct">токен отмены</param>
  [AllowAnonymous]
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
  [AllowAnonymous]
  [HttpGet("{eventId:Guid}")]
  [Tags("АПИ для событий")]
  [ProducesResponseType(typeof(EventInfoDTO), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> GetEventById([Required] Guid eventId, CancellationToken ct)
  {
    logger.LogDebug("Обработка запроса GET {methodName} по ID: {id} ", nameof(GetEventById), eventId);
    var responseEventDto = await eventService.GetEventAsync(eventId, ct);
    return responseEventDto is null ? NotFound() : Ok(responseEventDto);
  }

  /// <summary>
  /// Метод возвращает ТОП 10 событий
  /// </summary>
  /// <param name="ct">токен отмены</param>
  [AllowAnonymous]
  [HttpGet("top")]
  [Tags("АПИ для событий")]
  [ProducesResponseType(typeof(PaginatedResultTop10), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetTop10Events(CancellationToken ct)
  {
    logger.LogDebug("Обработка запроса GET {methodName}", nameof(GetTop10Events));

    var result = await eventService.GetTop10EventsAsync(ct);
    return Ok(result);
  }

  /// <summary>
  /// Метод создает мероприятие
  /// </summary>
  /// <param name="createEventDTO">Новое мероприятие</param>
  /// <param name="ct">токен отмены</param>
  [HttpPost]
  [Authorize(Roles = nameof(RoleType.Admin))]
  [Tags("АПИ для событий")]
  [ProducesResponseType(StatusCodes.Status201Created)]
  public async Task<IActionResult> AddEvent([FromBody] CreateEventDTO createEventDTO, CancellationToken ct)
  {
    logger.LogDebug("Обработка запроса POST {methodName}", nameof(AddEvent));
    var responseEventDto = await eventService.CreateEventAsync(createEventDTO, ct);
    return CreatedAtAction(nameof(GetEventById), new { eventId = responseEventDto.Id }, responseEventDto);
  }

  /// <summary>
  /// Метод обновления даных мероприятия
  /// </summary>
  /// <param name="eventId">идентификатор мероприятия</param>
  /// <param name="updateEventDto">данные для обновления</param>
  /// <param name="ct">токен отмены</param>
  [HttpPut("{eventId:Guid}")]
  [Authorize(Roles = nameof(RoleType.Admin))]
  [Tags("АПИ для событий")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> UpdateEvent([Required] Guid eventId, [FromBody, Required] UpdateEventDTO updateEventDto, CancellationToken ct)
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
  [Authorize(Roles = nameof(RoleType.Admin))]
  [Tags("АПИ для событий")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> DeleteEvent([Required] Guid eventId, CancellationToken ct)
  {
    logger.LogDebug("Обработка запроса DELETE {methodName} c id: {id}", nameof(DeleteEvent), eventId);
    await eventService.RemoveEventAsync(eventId, ct);
    return Ok();
  }
}