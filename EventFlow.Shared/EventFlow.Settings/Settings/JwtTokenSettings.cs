namespace EventFlow.Settings;

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
    public required string Secret { get; set; }

    /// <summary>
    /// кто выдал токен
    /// </summary>
    public required string Issuer { get; set; }

    /// <summary>
    /// для кого предназначен данный токен
    /// </summary>
    public required string Audience { get; set; }

    /// <summary>
    /// время жизни токена в минутах
    /// </summary>
    public int Lifetime { get; set; } = 2; 
}

