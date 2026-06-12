using Dapper;
using InnNou.Domain.Models;
using InnNou.Domain.Persistence;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InnNou.Infrastructure.Services;

public class AuthService(IDbConnectionFactory connectionFactory, IConfiguration configuration) : IAuthService
{
    public async Task<Login?> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var user = await connection.QueryFirstOrDefaultAsync<UserWithRoleResult>(
            "sp_Auth_GetUserByEmail",
            new { Email = email },
            commandType: CommandType.StoredProcedure);

        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        var jwt = GenerateJwtToken(user, user.RoleLevel, user.HotelId);
        var refreshTokenValue = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        await connection.ExecuteAsync(
            "sp_Auth_InsertRefreshToken",
            new { UserId = user.UserId, Token = refreshTokenValue, ExpiresAt = now.AddDays(7), CreatedAt = now },
            commandType: CommandType.StoredProcedure);

        return new Login
        {
            Token = jwt,
            RefreshToken = refreshTokenValue,
            Email = user.Email,
            UserId = user.UserId,
            UserToken = user.UserToken
        };
    }

    public async Task<Login?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var tokenData = await connection.QueryFirstOrDefaultAsync<RefreshTokenWithUserRoleResult>(
            "sp_Auth_GetRefreshTokenData",
            new { Token = refreshToken },
            commandType: CommandType.StoredProcedure);

        if (tokenData is null || tokenData.IsRevoked || tokenData.ExpiresAt < DateTime.UtcNow)
            return null;

        var newRefreshTokenValue = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await connection.ExecuteAsync(
                "sp_Auth_RevokeRefreshToken",
                new { Token = refreshToken, RevokedAt = now, ReplacedByToken = newRefreshTokenValue },
                transaction,
                commandType: CommandType.StoredProcedure);

            await connection.ExecuteAsync(
                "sp_Auth_InsertRefreshToken",
                new { UserId = tokenData.UserId, Token = newRefreshTokenValue, ExpiresAt = now.AddDays(7), CreatedAt = now },
                transaction,
                commandType: CommandType.StoredProcedure);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var jwt = GenerateJwtToken(tokenData.UserToken, tokenData.Email, tokenData.HotelId, tokenData.RoleLevel);

        return new Login
        {
            Token = jwt,
            RefreshToken = newRefreshTokenValue,
            Email = tokenData.Email,
            UserId = tokenData.UserId,
            UserToken = tokenData.UserToken
        };
    }

    public async Task<Login?> ImpersonateAsync(Guid actorUserToken, Guid targetUserToken, CancellationToken cancellationToken)
    {
        if (actorUserToken == targetUserToken)
            return null;

        await using var connection = connectionFactory.CreateConnection();

        var actor = await connection.QueryFirstOrDefaultAsync<UserWithRoleResult>(
            "sp_Auth_GetUserByToken",
            new { UserToken = actorUserToken },
            commandType: CommandType.StoredProcedure);

        var target = await connection.QueryFirstOrDefaultAsync<UserWithRoleResult>(
            "sp_Auth_GetUserByToken",
            new { UserToken = targetUserToken },
            commandType: CommandType.StoredProcedure);

        if (actor is null || target is null)
            return null;

        if (actor.RoleLevel <= target.RoleLevel)
            return null;

        if (actor.RoleLevel < 100 && actor.HotelId.HasValue)
        {
            var canAccess = await connection.ExecuteScalarAsync<int>(
                "sp_Hotel_IsInHierarchy",
                new { RootHotelId = actor.HotelId.Value, TargetHotelId = target.HotelId },
                commandType: CommandType.StoredProcedure);

            if (canAccess != 1)
                return null;
        }

        var jwt = GenerateJwtToken(
            actor.UserToken, actor.Email, target.HotelId, target.RoleLevel,
            impersonatedUserToken: target.UserToken,
            impersonatedEmail: target.Email,
            actorRoleLevel: actor.RoleLevel,
            actorHotelId: actor.HotelId);

        var refreshTokenValue = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        await connection.ExecuteAsync(
            "sp_Auth_InsertRefreshToken",
            new { UserId = actor.UserId, Token = refreshTokenValue, ExpiresAt = now.AddDays(7), CreatedAt = now },
            commandType: CommandType.StoredProcedure);

        return new Login
        {
            Token = jwt,
            RefreshToken = refreshTokenValue,
            Email = actor.Email,
            UserId = actor.UserId,
            UserToken = actor.UserToken
        };
    }

    public async Task<Login?> StopImpersonationAsync(Guid actorUserToken, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var actor = await connection.QueryFirstOrDefaultAsync<UserWithRoleResult>(
            "sp_Auth_GetUserByToken",
            new { UserToken = actorUserToken },
            commandType: CommandType.StoredProcedure);

        if (actor is null)
            return null;

        var jwt = GenerateJwtToken(actor.UserToken, actor.Email, actor.HotelId, actor.RoleLevel);
        var refreshTokenValue = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        await connection.ExecuteAsync(
            "sp_Auth_InsertRefreshToken",
            new { UserId = actor.UserId, Token = refreshTokenValue, ExpiresAt = now.AddDays(7), CreatedAt = now },
            commandType: CommandType.StoredProcedure);

        return new Login
        {
            Token = jwt,
            RefreshToken = refreshTokenValue,
            Email = actor.Email,
            UserId = actor.UserId,
            UserToken = actor.UserToken
        };
    }

    private string GenerateJwtToken(
        Guid userToken,
        string email,
        int? hotelId,
        int roleLevel,
        Guid? impersonatedUserToken = null,
        string? impersonatedEmail = null,
        int? actorRoleLevel = null,
        int? actorHotelId = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userToken.ToString()),
            new(ClaimTypes.NameIdentifier, userToken.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("roleLevel", roleLevel.ToString()),
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

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Overload for RefreshToken path where we only have the flat scalar fields (no UserWithRoleResult).
    private string GenerateJwtToken(UserWithRoleResult user, int roleLevel, int? hotelId)
        => GenerateJwtToken(user.UserToken, user.Email, hotelId, roleLevel);
}
