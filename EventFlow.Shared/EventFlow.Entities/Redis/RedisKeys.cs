namespace EventFlow.Entities.Redis;

public static class RedisKeys
{
    // ключ для Топ 10
    public const string TopEvents = "events:top10";

    //  ключ для одиночного события
    public static string ForEvent(Guid id) => $"event:{id}";

}