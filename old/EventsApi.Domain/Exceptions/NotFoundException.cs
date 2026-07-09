namespace EventsApi.Domain.Exceptions;

/// <summary>
/// Исключение, если объект(сущность) не найдены 
/// </summary>
public class EntityNotFoundException : Exception
{
    /// <summary> Название </summary>
    public string entityName { get; }
    /// <summary> Идентификатор </summary>
    public string entityId { get; }
    public EntityNotFoundException(string entityName, string entityId) : base($"Элемент {entityName} c идентификатором ID: '{entityId}' не найден.")
    {
        this.entityName = entityName;
        this.entityId = entityId;
    }
}