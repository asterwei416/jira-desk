using CallTrackingSystem.Core.Entities;

namespace CallTrackingSystem.Core.Interfaces;

/// <summary>
/// JWT Token 服務介面
/// </summary>
public interface IJwtTokenService
{
    string GenerateToken(User user);
    bool ValidateToken(string token);
}
