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

        public async Task<Login?> LoginAsync(string email, string password, CancellationToken cancellationToken)
        {
            var user = await (from u in _dbContext.Users
                              join r in _dbContext.Roles on u.RoleId equals r.RoleId
                              where u.Email == email
                              select new { User = u, Role = r })
                              .FirstOrDefaultAsync(cancellationToken);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.User.PasswordHash))
                return null;

            var jwt = GenerateJwtToken(
                user.User,
                user.Role.Level,
                user.User.HotelId
            );

            var refreshTokenValue = Guid.NewGuid().ToString();

            _dbContext.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.User.UserId,
                Token = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new Login
            {
                Token = jwt,
                RefreshToken = refreshTokenValue,
                Email = user.User.Email,
                UserId = user.User.UserId,
                UserToken = user.User.UserToken
            };
        }

        public async Task<Login?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            var tokenEntity = await (from r in _dbContext.RefreshTokens
                                     join u in _dbContext.Users on r.UserId equals u.UserId
                                     join role in _dbContext.Roles on u.RoleId equals role.RoleId
                                     where r.Token == refreshToken
                                     select new
                                     {
                                         RefreshToken = r,
                                         User = u,
                                         Role = role
                                     }).FirstOrDefaultAsync(cancellationToken);

            if (tokenEntity == null ||
                tokenEntity.RefreshToken.IsRevoked ||
                tokenEntity.RefreshToken.ExpiresAt < DateTime.UtcNow)
                return null;

            var user = tokenEntity.User;

            tokenEntity.RefreshToken.IsRevoked = true;

            var newRefreshTokenValue = Guid.NewGuid().ToString();

            _dbContext.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.UserId,
                Token = newRefreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            var jwt = GenerateJwtToken(
                user,
                tokenEntity.Role.Level,
                user.HotelId
            );

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new Login
            {
                Token = jwt,
                RefreshToken = newRefreshTokenValue,
                Email = user.Email,
                UserId = user.UserId,
                UserToken = user.UserToken
            };
        }

        public async Task<Login?> ImpersonateAsync(Guid actorUserToken, Guid targetUserToken, CancellationToken cancellationToken)
        {
            if (actorUserToken == targetUserToken)
                return null;

            var actorData = await (from u in _dbContext.Users
                                   join r in _dbContext.Roles on u.RoleId equals r.RoleId
                                   where u.UserToken == actorUserToken
                                   select new { User = u, Role = r })
                                   .FirstOrDefaultAsync(cancellationToken);

            var targetData = await (from u in _dbContext.Users
                                    join r in _dbContext.Roles on u.RoleId equals r.RoleId
                                    where u.UserToken == targetUserToken
                                    select new { User = u, Role = r })
                                    .FirstOrDefaultAsync(cancellationToken);

            if (actorData == null || targetData == null)
                return null;

            var actor = actorData.User;
            var actorRole = actorData.Role;

            var target = targetData.User;
            var targetRole = targetData.Role;

            if (actorRole.Level <= targetRole.Level)
                return null;

            if (!IsSuperAdmin(actorRole))
            {
                var canAccess = await IsSameOrChildHotel(actor.HotelId, target.HotelId, cancellationToken);

                if (!canAccess)
                    return null;
            }

            var jwt = GenerateJwtToken(
                actor,
                targetRole.Level,
                target.HotelId,
                impersonatedUserToken: target.UserToken,
                impersonatedEmail: target.Email,
                actorRoleLevel: actorRole.Level,
                actorHotelId: actor.HotelId
            );

            var refreshTokenValue = Guid.NewGuid().ToString();

            _dbContext.RefreshTokens.Add(new RefreshToken
            {
                UserId = actor.UserId,
                Token = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new Login
            {
                Token = jwt,
                RefreshToken = refreshTokenValue,
                Email = actor.Email,
                UserId = actor.UserId,
                UserToken = actor.UserToken
            };
        }

        public async Task<Login?> StopImpersonationAsync(Guid actorUserToken,  CancellationToken cancellationToken)
        {
            var actorData = await (from u in _dbContext.Users
                                   join r in _dbContext.Roles on u.RoleId equals r.RoleId
                                   where u.UserToken == actorUserToken
                                   select new { User = u, Role = r })
                                   .FirstOrDefaultAsync(cancellationToken);

            if (actorData == null)
                return null;

            var actor = actorData.User;
            var role = actorData.Role;

            var jwt = GenerateJwtToken(
                actor,
                role.Level,
                actor.HotelId
            );

            var refreshTokenValue = Guid.NewGuid().ToString();

            _dbContext.RefreshTokens.Add(new RefreshToken
            {
                UserId = actor.UserId,
                Token = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new Login
            {
                Token = jwt,
                RefreshToken = refreshTokenValue,
                Email = actor.Email,
                UserId = actor.UserId,
                UserToken = actor.UserToken
            };
        }


        private string GenerateJwtToken(User user, int roleLevel, int? hotelId, Guid? impersonatedUserToken = null, string? impersonatedEmail = null, int? actorRoleLevel = null, int? actorHotelId = null)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserToken.ToString()),

                new Claim(ClaimTypes.NameIdentifier, user.UserToken.ToString()),

                new Claim(JwtRegisteredClaimNames.Email, user.Email),

                new Claim("roleLevel", roleLevel.ToString()),
            };

            if (hotelId.HasValue)
                claims.Add(new Claim("hotelId", hotelId.Value.ToString()));

            if (impersonatedUserToken.HasValue)
            {
                claims.Add(new Claim("impersonatedUserToken", impersonatedUserToken.Value.ToString()));

                if (!string.IsNullOrEmpty(impersonatedEmail))
                    claims.Add(new Claim("impersonatedEmail", impersonatedEmail));

                if (actorRoleLevel.HasValue)
                    claims.Add(new Claim("actorRoleLevel", actorRoleLevel.Value.ToString()));

                if (actorHotelId.HasValue)
                    claims.Add(new Claim("actorHotelId", actorHotelId.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool IsSuperAdmin(Role role)
        {
            return role.Level >= 100;
        }

        private async Task<bool> IsSameOrChildHotel(int? actorHotelId, int? targetHotelId, CancellationToken ct)
        {
            if (actorHotelId == null)
                return true;

            if (actorHotelId == targetHotelId)
                return true;

            var current = await _dbContext.Hotels
                .FirstOrDefaultAsync(h => h.HotelId == targetHotelId, ct);

            while (current?.ParentHotelId != null)
            {
                if (current.ParentHotelId == actorHotelId)
                    return true;

                current = await _dbContext.Hotels
                    .FirstOrDefaultAsync(h => h.HotelId == current.ParentHotelId, ct);
            }

            return false;
        }
    }
}
