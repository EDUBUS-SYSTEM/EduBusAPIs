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
        private readonly IRefreshTokenRepository _refreshRepo;
        private readonly IConfiguration _config;

        public AuthService(IUserAccountRepository userRepo, IRefreshTokenRepository refreshRepo, IConfiguration config)
        {
            _userRepo = userRepo;
            _refreshRepo = refreshRepo;
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

            var (access, refresh, expires) = await GenerateTokensAsync(user, role);

            return new AuthResponse
            {
                Token = access,
                RefreshToken = refresh,
                FullName = $"{user.FirstName} {user.LastName}",
                Role = role,
                ExpiresAtUtc = expires
            };
        }

        public async Task LogoutAsync(Guid userId)
        {
            await _refreshRepo.InvalidateUserTokensAsync(userId);
        }

        public async Task<(string accessToken, string refreshToken, DateTime expiresUtc)?> RefreshTokensAsync(string refreshToken)
        {
            var tokenInDb = await _refreshRepo.GetByTokenAsync(refreshToken);

            if (tokenInDb == null || tokenInDb.ExpiresAtUtc <= DateTime.UtcNow || tokenInDb.RevokedAtUtc != null)
                return null;

            var user = await _userRepo.FindAsync(tokenInDb.UserId);
            if (user == null) return null;

            var role = user switch
            {
                Admin => "Admin",
                Driver => "Driver",
                Parent => "Parent",
                _ => "Unknown"
            };

            return await GenerateTokensAsync(user, role);
        }

        private async Task<(string token, string refresh, DateTime expiresUtc)> GenerateTokensAsync(UserAccount user, string role)
        {
            var (access, expires) = GenerateJwt(user, role,
                int.Parse(_config["Jwt:AccessTokenMinutes"] ?? "15"));

            var refresh = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = Guid.NewGuid().ToString("N"),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(
                    int.Parse(_config["Jwt:RefreshTokenDays"] ?? "7"))
            };

            await _refreshRepo.AddAsync(refresh);

            await _refreshRepo.EnforceUserTokenLimitAsync(user.Id, 3);

            return (access, refresh.Token, expires);
        }

        private (string token, DateTime expiresUtc) GenerateJwt(UserAccount user, string role, int ttlMinutes)
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

            var expires = DateTime.UtcNow.AddMinutes(ttlMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }

        private bool VerifyPassword(string plainPassword, byte[] hashedBytes)
        {
            var hashString = Encoding.UTF8.GetString(hashedBytes);
            return BCrypt.Net.BCrypt.Verify(plainPassword, hashString);
        }

        public async Task<UserAccount?> GetUserByIdAsync(Guid userId)
        {
            return await _userRepo.FindAsync(userId);
        }

    }
}
