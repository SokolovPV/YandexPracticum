namespace EventsApi.Infrastructure.Options;

/// <summary>
/// Класс настроек для генерации токена
/// </summary>
public class JwtTokenSettings
{
    /// <summary>
    /// название схемы аутентификации
    /// </summary>
    public required string SchemeName { get; set; }
    /// <summary>
    /// секрет
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// кто выдал токен
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// для кого предназначен данный токен
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// время жизни токена в минутах
    /// </summary>
    public int Lifetime { get; set; }
}

