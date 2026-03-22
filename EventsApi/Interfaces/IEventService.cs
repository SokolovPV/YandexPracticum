using EventsApi.ModelDTO;
using EventsApi.Models;

namespace EventsApi.Interfaces
{
	public interface IEventService
	{
    PaginatedResult GetEvents(string? title, DateTime? from, DateTime? to, int? page, int? pageSize);
		EventDTO? GetEvent(Guid id);
		EventDTO AddEvent(InputEventDTO createEventDTO);
		bool ChangeEvent(Guid id, InputEventDTO updateEvent);
		bool RemoveEvent(Guid id);
	}
}
