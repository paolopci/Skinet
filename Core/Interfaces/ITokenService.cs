using Core.Entities;

namespace Core.Interfaces;

public interface ITokenService
{
    string CreateToken(AppUser user);
    (RefreshToken refreshToken, string rawToken) CreateRefreshToken(AppUser user, string? ipAddress, string? userAgent);
    string HashToken(string token);
}
