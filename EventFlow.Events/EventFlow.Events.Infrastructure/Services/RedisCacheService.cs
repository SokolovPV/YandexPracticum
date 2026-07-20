using EventFlow.Events.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EventFlow.Events.Infrastructure.Services;

public class RedisCacheService(IConnectionMultiplexer connection, ILogger<RedisCacheService> logger) : ICacheService
{
    private IDatabase? GetRedisDatabase()
    {
        try
        {
            return connection.GetDatabase();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении базы данных Redis.");
            return null;
        }
    }

    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            var db = GetRedisDatabase();
            if (db == null)
                return null;

            return await db.StringGetAsync(key);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Ошибка получения данных из БД Redis по ключу {Key}.", key);
            return null;
        }
    }

    public async Task KeyDeleteAsync(string key)
    {
        try
        {
            var db = GetRedisDatabase();
            if (db == null)
                return;

            await db.KeyDeleteAsync(key);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Ошибка удаления данных из БД Redis по ключу {Key}.", key);
        }
    }

    public async Task SetStringAsync(string key, string value, TimeSpan expirationtime)
    {
        try
        {
            var db = GetRedisDatabase();
            if (db == null)
                return;

            await db.StringSetAsync(key, value, expirationtime);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Ошибка записи в БД Redis по ключу {Key}.", key);
        }
    }
}