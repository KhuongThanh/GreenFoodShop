using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auth.API.Data;
using Auth.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Auth.API.Services
{
    public interface ITokenService
    {
        Task<(string accessToken, string refreshToken)> CreateTokensAsync(User user, string? ipAddress = null);
    }

    public class TokenService : ITokenService
    {
        private readonly JwtSettings _settings;
        private readonly AuthDbContext _db;

        public TokenService(IOptions<JwtSettings> settings, AuthDbContext db)
        {
            _settings = settings.Value;
            _db = db;
        }

        public async Task<(string accessToken, string refreshToken)> CreateTokensAsync(User user, string? ipAddress = null)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new(JwtRegisteredClaimNames.Email, user.Email),
            };

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes),
                signingCredentials: creds);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshEntity = new RefreshToken
            {
                TokenId = Guid.NewGuid(),
                UserId = user.UserId,
                TokenHash = HashRefreshToken(refreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(_settings.RefreshTokenDays),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };

            _db.RefreshTokens.Add(refreshEntity);
            await _db.SaveChangesAsync();

            return (accessToken, refreshToken);
        }

        private static string HashRefreshToken(string token)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}
