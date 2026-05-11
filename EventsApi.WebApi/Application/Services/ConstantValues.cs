namespace EventsApi.WebApi.Application.Services;

public static class ConstantValues
{
   public const string key_not_found_exception = "Идентификатор мероприятия не найден.";
   public const string dateFrom_more_dateTo_exception = "Дата начала мероприятия больше даты завершения.";
   public const string totalSeats_more_range_exception = "Общее количество мест на событие должно быть больше 1 и меньше 100.";
}