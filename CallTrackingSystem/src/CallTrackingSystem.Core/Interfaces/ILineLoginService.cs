using CallTrackingSystem.Core.DTOs;

namespace CallTrackingSystem.Core.Interfaces;

/// <summary>
/// LINE Login 服務介面
/// </summary>
public interface ILineLoginService
{
    string BuildLoginUrl(string state, string redirectUri);
    Task<LineLoginProfile> GetUserProfileAsync(string code, string redirectUri, CancellationToken cancellationToken = default);
}
