using EventsApi.Models.ModelDTO.Event;

namespace EventsApi.Application.Interfaces
{
    public interface IEventService
    {
        PaginatedResult GetEvents(string? title, DateTime? from, DateTime? to, int? page, int? pageSize);
        ResponceEventDTO? GetEvent(Guid id);
        ResponceEventDTO AddEvent(InputEventDTO createEventDTO);
        bool ChangeEvent(Guid id, InputEventDTO updateEvent);
        bool RemoveEvent(Guid id);
    }
}
