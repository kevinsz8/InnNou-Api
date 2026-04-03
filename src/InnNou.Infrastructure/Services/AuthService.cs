using InnNou.Domain.Models;
using InnNou.Domain.Persistence;
using InnNou.Infrastructure.Repositories.DbContexts;
using InnNou.Infrastructure.Repositories.DbEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InnNou.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly InnNouDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public AuthService(InnNouDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task<InnNou.Domain.Models.Login?> LoginAsync(string email, string password, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                //new Claim("tenant_id", user.TenantId.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            var refreshTokenValue = Guid.NewGuid().ToString();

            var refreshToken = new RefreshToken
            {
                UserId = user.UserId,
                Token = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new Login
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshTokenValue,
                Email = user.Email,
                UserId = user.UserId,
                UserToken = user.UserToken
            };
        }

        public async Task<Login?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            var tokenEntity = await (from r in _dbContext.RefreshTokens
                join u in _dbContext.Users on r.UserId equals u.UserId
                where r.Token == refreshToken
                select new {
                    RefreshToken = r,
                    User = u
                }
                ).FirstOrDefaultAsync(cancellationToken);

            if (tokenEntity == null || tokenEntity.RefreshToken.IsRevoked || tokenEntity.RefreshToken.ExpiresAt < DateTime.UtcNow)
                return null;

            var user = tokenEntity.User;

            // 🔥 OPCIONAL (RECOMENDADO): invalidar el anterior
            tokenEntity.RefreshToken.IsRevoked = true;

            var newRefreshTokenValue = Guid.NewGuid().ToString();

            var newRefreshToken = new RefreshToken
            {
                UserId = user.UserId,
                Token = newRefreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _dbContext.RefreshTokens.Add(newRefreshToken);

            // 🔥 NUEVO JWT
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new Login
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = newRefreshTokenValue,
                Email = user.Email,
                UserId = user.UserId,
                UserToken = user.UserToken
            };
        }
    }
}
