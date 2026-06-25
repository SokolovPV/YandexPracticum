using System.Security.Claims;
using EventsApi.Application.Interfaces;
using EventsApi.Domain.Entities;
using EventsApi.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace EventsApi.Infrastructure.Services;

public class JwtTokenGenerator(IOptions<JwtTokenSettings> options) : ITokenGenerator
{
    private readonly JwtTokenSettings jwtTokenSettings = options.Value;
    public string GenerateToken(User user, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var lifeTime = now.AddMinutes(jwtTokenSettings.Lifetime);
        var claims = new[]
        {
            // идентификатор пользователя
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            // роль
            new Claim("role", user.Role.ToString()),
            // ID токена
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtTokenSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = jwtTokenSettings.Issuer,
            Audience = jwtTokenSettings.Audience,
            NotBefore = now,
            Expires = lifeTime,
            IssuedAt = now,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(securityToken);
    }
}