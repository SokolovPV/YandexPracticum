using EventsApi.Models.Domain;
using EventsApi.Models.ModelDTO;

namespace EventsApi.Application.Interfaces
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
