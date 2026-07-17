namespace EventFlow.Events.Application.Interfaces;

public interface ICacheService
{
    /// <summary>
    /// Получить значение из кеш
    /// </summary>
    /// <param name="key">Ключ значения в кэше</param>
    Task<string?> GetStringAsync(string key);

    /// <summary>
    /// Записать значение в кэш с TTL
    /// </summary>
    /// <param name="key">Ключ значения в кэше</param>
    /// <param name="value">Значение для записи в кэш</param>
    /// <param name="expirationtime">Время жизни значения в кэше</param>
    Task SetStringAsync(string key, string value, TimeSpan expirationtime);

    /// <summary>
    /// Удалить значение из кэш
    /// </summary>
    /// <param name="key">Ключ значения в кэше</param>
    Task KeyDeleteAsync(string key);
}