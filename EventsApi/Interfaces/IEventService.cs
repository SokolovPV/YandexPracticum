using EventsApi.ModelDTO;
using EventsApi.Models;

namespace EventsApi.Interfaces
{
	public interface IEventService
	{
		List<EventDTO> GetEvents();
    EventDTO? GetEvent(Guid id);
    EventDTO AddEvent(InputEventDTO createEventDTO);
		bool ChangeEvent(Guid id, InputEventDTO updateEvent);
		bool RemoveEvent(Guid id);
	}
}
