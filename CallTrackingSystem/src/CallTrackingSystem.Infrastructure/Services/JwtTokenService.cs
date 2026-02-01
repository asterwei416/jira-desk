using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CallTrackingSystem.Infrastructure.Services;

/// <summary>
/// JWT Token 服務
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _secretKey = configuration["JwtSettings:SecretKey"] ?? string.Empty;
        _issuer = configuration["JwtSettings:Issuer"] ?? string.Empty;
        _audience = configuration["JwtSettings:Audience"] ?? string.Empty;

        if (!int.TryParse(configuration["JwtSettings:ExpirationMinutes"], out _expirationMinutes))
        {
            _expirationMinutes = 1440;
        }

        if (string.IsNullOrWhiteSpace(_secretKey) || _secretKey.Length < 32)
        {
            throw new InvalidOperationException("JWT SecretKey 設定不正確（需至少 32 字元）");
        }
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.LineUserId))
        {
            claims.Add(new Claim("lineUserId", user.LineUserId!));
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
