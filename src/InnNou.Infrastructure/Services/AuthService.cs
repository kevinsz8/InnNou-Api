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
using System.Security.Cryptography;
using System.Text;

namespace InnNou.Infrastructure.Services;

public class AuthService(IDbConnectionFactory connectionFactory, IConfiguration configuration) : IAuthService
{
    public async Task<Login?> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var user = await connection.QueryFirstOrDefaultAsync<UserWithRoleResult>(
            "sp_Auth_GetUserByEmail",
            new { Email = email.ToUpperInvariant() },
            commandType: CommandType.StoredProcedure);

        if (user is null || user.IsDeleted || !user.IsActive)
            return null;

        if (user.LockedUntilUtc.HasValue && user.LockedUntilUtc.Value > DateTime.UtcNow)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            await connection.ExecuteAsync(
                "sp_Auth_OnLoginFailure",
                new { UserId = user.UserId },
                commandType: CommandType.StoredProcedure);
            return null;
        }

        await connection.ExecuteAsync(
            "sp_Auth_OnLoginSuccess",
            new { UserId = user.UserId, LastLoginUtc = DateTime.UtcNow },
            commandType: CommandType.StoredProcedure);

        var jwt = GenerateJwtToken(user.UserToken, user.Email, user.OrganizationId, user.SupplierId, user.RoleLevel);

        var (plainToken, tokenHash, tokenId) = GenerateRefreshToken();
        var now = DateTime.UtcNow;

        await connection.ExecuteAsync(
            "sp_Auth_InsertRefreshToken",
            new { RefreshTokenToken = tokenId, UserId = user.UserId, TokenHash = tokenHash, ExpiresUtc = now.AddDays(7), CreatedUtc = now },
            commandType: CommandType.StoredProcedure);

        return new Login
        {
            Token = jwt,
            RefreshToken = plainToken,
            Email = user.Email,
            UserId = user.UserId,
            UserToken = user.UserToken
        };
    }

    public async Task<Login?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var tokenHash = HashToken(refreshToken);

        var tokenData = await connection.QueryFirstOrDefaultAsync<RefreshTokenWithUserRoleResult>(
            "sp_Auth_GetRefreshTokenData",
            new { TokenHash = tokenHash },
            commandType: CommandType.StoredProcedure);

        if (tokenData is null || tokenData.IsRevoked || tokenData.ExpiresUtc < DateTime.UtcNow)
            return null;

        var (newPlainToken, newTokenHash, newTokenId) = GenerateRefreshToken();
        var now = DateTime.UtcNow;

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await connection.ExecuteAsync(
                "sp_Auth_RevokeRefreshToken",
                new { TokenHash = tokenHash, RevokedUtc = now, ReplacedByToken = newTokenId },
                transaction,
                commandType: CommandType.StoredProcedure);

            await connection.ExecuteAsync(
                "sp_Auth_InsertRefreshToken",
                new { RefreshTokenToken = newTokenId, UserId = tokenData.UserId, TokenHash = newTokenHash, ExpiresUtc = now.AddDays(7), CreatedUtc = now },
                transaction,
                commandType: CommandType.StoredProcedure);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var jwt = GenerateJwtToken(tokenData.UserToken, tokenData.Email, tokenData.OrganizationId, tokenData.SupplierId, tokenData.RoleLevel);

        return new Login
        {
            Token = jwt,
            RefreshToken = newPlainToken,
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

        if (!actor.CanImpersonate)
            return null;

        if (actor.RoleLevel <= target.RoleLevel)
            return null;

        // Supplier users can only be impersonated by superadmin
        if (target.SupplierId.HasValue && !target.OrganizationId.HasValue && actor.RoleLevel < 100)
            return null;

        if (actor.RoleLevel < 100 && actor.OrganizationId.HasValue)
        {
            var canAccess = await connection.ExecuteScalarAsync<int>(
                "sp_Organization_IsInHierarchy",
                new { RootOrganizationId = actor.OrganizationId.Value, TargetOrganizationId = target.OrganizationId },
                commandType: CommandType.StoredProcedure);

            if (canAccess != 1)
                return null;
        }

        await connection.ExecuteAsync(
            "sp_Auth_InsertImpersonationSession",
            new { ActorUserId = actor.UserId, TargetUserId = target.UserId, StartedUtc = DateTime.UtcNow },
            commandType: CommandType.StoredProcedure);

        var jwt = GenerateJwtToken(
            actor.UserToken, actor.Email, target.OrganizationId, target.SupplierId, target.RoleLevel,
            impersonatedUserToken: target.UserToken,
            impersonatedEmail: target.Email,
            actorRoleLevel: actor.RoleLevel,
            actorOrganizationId: actor.OrganizationId);

        var (plainToken, tokenHash, tokenId) = GenerateRefreshToken();
        var now = DateTime.UtcNow;

        await connection.ExecuteAsync(
            "sp_Auth_InsertRefreshToken",
            new { RefreshTokenToken = tokenId, UserId = actor.UserId, TokenHash = tokenHash, ExpiresUtc = now.AddDays(7), CreatedUtc = now },
            commandType: CommandType.StoredProcedure);

        return new Login
        {
            Token = jwt,
            RefreshToken = plainToken,
            Email = actor.Email,
            UserId = actor.UserId,
            UserToken = actor.UserToken
        };
    }

    public async Task<Login?> ImpersonateSupplierAsync(Guid actorUserToken, Guid supplierToken, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var linkedUser = await connection.QueryFirstOrDefaultAsync<UserWithRoleResult>(
            "sp_Auth_GetUserBySupplierToken",
            new { SupplierToken = supplierToken },
            commandType: CommandType.StoredProcedure);

        if (linkedUser is null)
            return null;

        var result = await ImpersonateAsync(actorUserToken, linkedUser.UserToken, cancellationToken);

        if (result is not null)
            result.SupplierName = linkedUser.SupplierName;

        return result;
    }

    public async Task<Login?> ImpersonateWarehouseContactAsync(Guid actorUserToken, Guid warehouseContactToken, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var linkedUser = await connection.QueryFirstOrDefaultAsync<UserWithRoleResult>(
            "sp_Auth_GetUserByWarehouseContactToken",
            new { WarehouseContactToken = warehouseContactToken },
            commandType: CommandType.StoredProcedure);

        if (linkedUser is null)
            return null;

        var result = await ImpersonateAsync(actorUserToken, linkedUser.UserToken, cancellationToken);

        if (result is not null)
            result.WarehouseContactName = linkedUser.WarehouseContactName;

        return result;
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

        await connection.ExecuteAsync(
            "sp_Auth_EndImpersonationSession",
            new { ActorUserId = actor.UserId, EndedUtc = DateTime.UtcNow },
            commandType: CommandType.StoredProcedure);

        var jwt = GenerateJwtToken(actor.UserToken, actor.Email, actor.OrganizationId, actor.SupplierId, actor.RoleLevel);

        var (plainToken, tokenHash, tokenId) = GenerateRefreshToken();
        var now = DateTime.UtcNow;

        await connection.ExecuteAsync(
            "sp_Auth_InsertRefreshToken",
            new { RefreshTokenToken = tokenId, UserId = actor.UserId, TokenHash = tokenHash, ExpiresUtc = now.AddDays(7), CreatedUtc = now },
            commandType: CommandType.StoredProcedure);

        return new Login
        {
            Token = jwt,
            RefreshToken = plainToken,
            Email = actor.Email,
            UserId = actor.UserId,
            UserToken = actor.UserToken
        };
    }

    private string GenerateJwtToken(
        Guid userToken,
        string email,
        int? organizationId,
        int? supplierId,
        int roleLevel,
        Guid? impersonatedUserToken = null,
        string? impersonatedEmail = null,
        int? actorRoleLevel = null,
        int? actorOrganizationId = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userToken.ToString()),
            new(ClaimTypes.NameIdentifier, userToken.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("roleLevel", roleLevel.ToString()),
        };

        if (organizationId.HasValue)
            claims.Add(new Claim("organizationId", organizationId.Value.ToString()));

        if (supplierId.HasValue)
            claims.Add(new Claim("supplierId", supplierId.Value.ToString()));

        if (impersonatedUserToken.HasValue)
        {
            claims.Add(new Claim("impersonatedUserToken", impersonatedUserToken.Value.ToString()));

            if (!string.IsNullOrEmpty(impersonatedEmail))
                claims.Add(new Claim("impersonatedEmail", impersonatedEmail));

            if (actorRoleLevel.HasValue)
                claims.Add(new Claim("actorRoleLevel", actorRoleLevel.Value.ToString()));

            if (actorOrganizationId.HasValue)
                claims.Add(new Claim("actorOrganizationId", actorOrganizationId.Value.ToString()));
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

    private static (string plainToken, string tokenHash, Guid tokenId) GenerateRefreshToken()
    {
        var tokenId = Guid.NewGuid();
        var plainToken = tokenId.ToString("N");
        var tokenHash = HashToken(plainToken);
        return (plainToken, tokenHash, tokenId);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
