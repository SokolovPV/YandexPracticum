using EventsApi.Application.Interfaces;
using EventsApi.Models.Domain;
using EventsApi.Models.ModelDTO.Event;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventsApi.Controllers
{
  /// <summary>
  /// Контроллер для работы с мероприятиями
  /// </summary>
  [Route("[controller]")]
  [ApiController]
  public class EventsController(IEventService _eventService) : ControllerBase
  {

    /// <summary>
    /// Метод возвращает список мероприятий
    /// </summary>
    /// <param name="title">поиск по названию</param>
    /// <param name="from"> события, которые начинаются не раньше указанной даты</param>
    /// <param name="to">события, которые заканчиваются не позже указанной даты</param>
    /// <param name="page"> страница, которую необходимо вернуть</param>
    /// <param name="pageSize">количество элементов на странице</param>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PaginatedResult), StatusCodes.Status200OK)]
    public ActionResult<List<Event>> GetEvents(string? title, DateTime? from, DateTime? to, int? page, int? pageSize)
    {
      return Ok(_eventService.GetEvents(title, from, to, page, pageSize));
    }

    /// <summary>
    /// Метод возвращает мероприятие по идентификатору из списка
    /// </summary>
    /// <param name="id">Параметр идентификатор мероприятия</param>
    [HttpGet("{id:Guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(EventDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetEventById(Guid id)
    {
      var eventDto = _eventService.GetEvent(id);
      return eventDto is null ? NotFound() : Ok(eventDto);
    }

    /// <summary>
    /// Метод создает мероприятие
    /// </summary>
    /// <param name="createEventDTO">Новое мероприятие</param>
    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public IActionResult AddEvent([FromBody] InputEventDTO createEventDTO)
    {
      var eventDto = _eventService.AddEvent(createEventDTO);
      return CreatedAtAction(nameof(GetEventById), new { id = eventDto.Id }, eventDto);
    }

    /// <summary>
    /// Метод обновления даных мероприятия
    /// </summary>
		/// <param name="id">идентификатор мероприятия</param>
    /// <param name="updateDto">данне для обновления</param>
    [HttpPut("{id:Guid}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UpdateEvent(Guid id, [FromBody] InputEventDTO updateDto)
    {
      var state = _eventService.ChangeEvent(id, updateDto);
      return state ? Ok() : NotFound();
    }

    /// <summary>
    /// Метод удаляет мероприятие по идентификатору из списка
    /// </summary>
    /// <param name="id">Параметр идентификатор мероприятия</param>
    [HttpDelete("{id:Guid}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteEvent(Guid id)
    {
      var state = _eventService.RemoveEvent(id);
      return state ? Ok() : NotFound();
    }
  }
}
