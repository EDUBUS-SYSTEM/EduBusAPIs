using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Services.Contracts;
using Services.Models.UserAccount;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserAccountRepository _userRepo;
        private readonly IConfiguration _config;

        private static readonly Dictionary<Guid, string> _refreshTokens = new();

        public AuthService(IUserAccountRepository userRepo, IConfiguration config)
        {
            _userRepo = userRepo;
            _config = config;
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userRepo.GetByEmailAsync(request.Email);
            if (user is null) return null;

            if (!VerifyPassword(request.Password, user.HashedPassword))
                return null;

            var role = user switch
            {
                Admin => "Admin",
                Driver => "Driver",
                Parent => "Parent",
                _ => "Unknown"
            };

            var (access, refresh, expires) = GenerateTokens(user, role);

            return new AuthResponse
            {
                Token = access,
                RefreshToken = refresh,
                FullName = $"{user.FirstName} {user.LastName}",
                Role = role,
                ExpiresAtUtc = expires
            };
        }

        public bool VerifyPassword(string plainPassword, byte[] hashedBytes)
        {
            var hashString = Encoding.UTF8.GetString(hashedBytes);
            return BCrypt.Net.BCrypt.Verify(plainPassword, hashString);
        }

        public (string accessToken, string refreshToken, DateTime expiresUtc) GenerateTokens(UserAccount user, string role)
        {
            var (token, expires) = GenerateJwt(user, role);
            var refresh = Guid.NewGuid().ToString("N");

            _refreshTokens[user.Id] = refresh;

            return (token, refresh, expires);
        }

        public void InvalidateRefreshToken(Guid userId)
        {
            _refreshTokens.Remove(userId);
        }

        public bool ValidateRefreshToken(Guid userId, string refreshToken)
        {
            return _refreshTokens.TryGetValue(userId, out var saved) && saved == refreshToken;
        }

        public UserAccount? GetUserById(Guid userId)
        {
            return _userRepo.FindAsync(userId).Result;
        }

        private (string token, DateTime expiresUtc) GenerateJwt(UserAccount user, string role)
        {
            var keyStr = _config["Jwt:Key"];
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role)
            };

            var expires = DateTime.UtcNow.AddHours(4);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }
    }
}
