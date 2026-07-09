using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Persistence;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Models;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class UserService(IDbConnectionFactory connectionFactory, IMapper mapper) : IUserService
{
    private sealed class UserPageRow : User { public int TotalCount { get; set; } }

    public async Task<UserDto?> CreateUserAsync(UserDto userDto, IRequestContext context, CancellationToken cancellationToken)
    {
        if (userDto.OrganizationId.HasValue && userDto.SupplierId.HasValue)
            throw new ApiException(ErrorCodes.UserOrgAndSupplierConflict, "A user cannot belong to both an organization and a supplier", 400);

        await using var connection = connectionFactory.CreateConnection();

        var role = await connection.QueryFirstOrDefaultAsync<Role>(
            "sp_Role_GetById",
            new { RoleId = userDto.RoleId },
            commandType: CommandType.StoredProcedure);

        if (role is null)
            throw new ApiException(ErrorCodes.UserInvalidRole, "Invalid role", 400);

        if (role.RoleLevel > context.RoleLevel)
            throw new ApiException(ErrorCodes.UserCannotAssignHigherRole, "Cannot assign higher role", 403);

        if (context.RoleLevel < 100)
        {
            if (userDto.SupplierId.HasValue)
                throw new ApiException(ErrorCodes.UserSupplierCreateSuperadminOnly, "Only superadmin can create supplier users", 403);

            if (!context.OrganizationId.HasValue)
                throw new ApiException(ErrorCodes.UserInvalidOrganizationContext, "Invalid organization context", 400);

            if (!userDto.OrganizationId.HasValue)
                throw new ApiException(ErrorCodes.UserInvalidOrganizationAssignment, "Invalid organization assignment", 400);

            var canAccess = await connection.ExecuteScalarAsync<int>(
                "sp_Organization_IsInHierarchy",
                new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = userDto.OrganizationId.Value },
                commandType: CommandType.StoredProcedure);

            if (canAccess != 1)
                throw new ApiException(ErrorCodes.UserInvalidOrganizationAssignment, "Invalid organization assignment", 400);
        }

        var createdUser = await connection.QueryFirstOrDefaultAsync<User>(
            "sp_User_Create",
            new
            {
                UserToken = Guid.NewGuid(),
                userDto.FirstName,
                userDto.LastName,
                Email = userDto.Email,
                NormalizedEmail = userDto.Email.ToUpperInvariant(),
                UserName = userDto.UserName,
                NormalizedUserName = userDto.UserName.ToUpperInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                userDto.RoleId,
                userDto.OrganizationId,
                userDto.SupplierId,
                IsActive = true,
                IsDeleted = false,
                CreatedUtc = DateTime.UtcNow,
                CreatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return createdUser is null ? null : mapper.Map<UserDto>(createdUser);
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(
        int pageNumber,
        int pageSize,
        string? searchField,
        string? searchText,
        bool includeInactive,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        await using var connection = connectionFactory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@ContextRoleLevel", context.RoleLevel);
        p.Add("@RootOrganizationId", context.RoleLevel >= 100 ? (int?)null : context.OrganizationId);
        p.Add("@SupplierId", context.RoleLevel < 100 && context.SupplierId.HasValue ? context.SupplierId : (int?)null);
        p.Add("@SearchField", string.IsNullOrWhiteSpace(searchField) ? null : searchField.Trim().ToLower());
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@IncludeInactive", includeInactive);

        var rows = (await connection.QueryAsync<UserPageRow>(
            "sp_User_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<UserDto>
        {
            Items = mapper.MapList<UserDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<UserDto?> EditUserAsync(UserDto request, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<UserWithRoleResult>(
            "sp_User_GetByToken",
            new { UserToken = request.UserToken },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        if (existing.RoleLevel > context.RoleLevel)
            throw new ApiException(ErrorCodes.UserCannotEditHigherRole, "Cannot edit higher role", 403);

        if (context.RoleLevel < 100 && context.OrganizationId.HasValue)
        {
            if (!existing.OrganizationId.HasValue)
                throw new ApiException(ErrorCodes.UserOutsideOrganization, "Cannot edit user from another organization", 403);

            var canAccess = await connection.ExecuteScalarAsync<int>(
                "sp_Organization_IsInHierarchy",
                new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = existing.OrganizationId.Value },
                commandType: CommandType.StoredProcedure);

            if (canAccess != 1)
                throw new ApiException(ErrorCodes.UserOutsideOrganization, "Cannot edit user from another organization", 403);
        }

        var newRoleId = existing.RoleId;
        if (request.RoleId != 0 && request.RoleId != existing.RoleId)
        {
            var newRole = await connection.QueryFirstOrDefaultAsync<Role>(
                "sp_Role_GetById",
                new { RoleId = request.RoleId },
                commandType: CommandType.StoredProcedure);

            if (newRole is null)
                throw new ApiException(ErrorCodes.UserInvalidRole, "Invalid role", 400);

            if (newRole.RoleLevel > context.RoleLevel)
                throw new ApiException(ErrorCodes.UserCannotAssignHigherRole, "Cannot assign higher role", 403);

            newRoleId = newRole.RoleId;
        }

        var newEmail = !string.IsNullOrWhiteSpace(request.Email) ? request.Email : existing.Email;
        var newUserName = !string.IsNullOrWhiteSpace(request.UserName) ? request.UserName : existing.UserName;

        var updatedUser = await connection.QueryFirstOrDefaultAsync<User>(
            "sp_User_Update",
            new
            {
                UserToken = request.UserToken,
                Email = newEmail,
                NormalizedEmail = newEmail.ToUpperInvariant(),
                FirstName = !string.IsNullOrWhiteSpace(request.FirstName) ? request.FirstName : existing.FirstName,
                LastName = !string.IsNullOrWhiteSpace(request.LastName) ? request.LastName : existing.LastName,
                UserName = newUserName,
                NormalizedUserName = newUserName.ToUpperInvariant(),
                PasswordHash = !string.IsNullOrWhiteSpace(request.Password)
                    ? BCrypt.Net.BCrypt.HashPassword(request.Password)
                    : existing.PasswordHash,
                RoleId = newRoleId,
                OrganizationId = existing.OrganizationId,
                LastUpdatedUtc = DateTime.UtcNow,
                LastUpdatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return updatedUser is null ? null : mapper.Map<UserDto>(updatedUser);
    }

    public async Task<bool> DeleteUserAsync(Guid userToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<UserWithRoleResult>(
            "sp_User_GetByToken",
            new { UserToken = userToken },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return false;

        if (existing.RoleLevel > context.RoleLevel)
            throw new ApiException(ErrorCodes.UserCannotDeleteHigherRole, "Cannot delete higher role", 403);

        if (context.RoleLevel < 100 && context.OrganizationId.HasValue)
        {
            if (!existing.OrganizationId.HasValue)
                throw new ApiException(ErrorCodes.UserOutsideOrganization, "Cannot delete user from another organization", 403);

            var canAccess = await connection.ExecuteScalarAsync<int>(
                "sp_Organization_IsInHierarchy",
                new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = existing.OrganizationId.Value },
                commandType: CommandType.StoredProcedure);

            if (canAccess != 1)
                throw new ApiException(ErrorCodes.UserOutsideOrganization, "Cannot delete user from another organization", 403);
        }

        var now = DateTime.UtcNow;
        var actor = context.ActorUserToken.ToString();

        await connection.ExecuteAsync(
            "sp_User_SoftDelete",
            new
            {
                UserToken = userToken,
                IsDeleted = true,
                DeletedUtc = now,
                DeletedBy = actor,
                LastUpdatedUtc = now,
                LastUpdatedBy = actor
            },
            commandType: CommandType.StoredProcedure);

        return true;
    }

    public async Task<UserDto?> GetUserByTokenAsync(Guid userToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<UserWithRoleResult>(
            "sp_User_GetByToken",
            new { UserToken = userToken },
            commandType: CommandType.StoredProcedure);

        if (existing is null || existing.IsDeleted)
            return null;

        if (context.RoleLevel < 100)
        {
            if (context.SupplierId.HasValue)
            {
                if (existing.SupplierId != context.SupplierId)
                    return null;
            }
            else if (context.OrganizationId.HasValue)
            {
                if (!existing.OrganizationId.HasValue)
                    return null;

                var canAccess = await connection.ExecuteScalarAsync<int>(
                    "sp_Organization_IsInHierarchy",
                    new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = existing.OrganizationId.Value },
                    commandType: CommandType.StoredProcedure);

                if (canAccess != 1)
                    return null;
            }
        }

        return mapper.Map<UserDto>(existing);
    }

    public async Task<bool> IsUserExists(string email, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var result = await connection.ExecuteScalarAsync<int>(
            "sp_User_ExistsByEmail",
            new { NormalizedEmail = email.ToUpperInvariant() },
            commandType: CommandType.StoredProcedure);

        return result == 1;
    }
}
