namespace EventFlow.Bookings.Domain.Exceptions;
/// <summary>
/// Исключение, когда идентификатор не найден 
/// </summary>
public class KeyNotExistException :  Exception
{
  public string Id { get; }
  public string EntityName { get; }

  public KeyNotExistException(string id, string entityName) : base($"Элемент {entityName} c ID: '{id}' не найден.")
  {
    this.Id = id;
    this.EntityName = entityName;
  }
}