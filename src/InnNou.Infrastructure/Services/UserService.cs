using AutoMapper;
using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Persistence;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Models;
using InnNou.Infrastructure.Repositories.DbEntities;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class UserService(IDbConnectionFactory connectionFactory, IMapper mapper) : IUserService
{
    public async Task<UserDto?> CreateUserAsync(UserDto userDto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var role = await connection.QueryFirstOrDefaultAsync<Role>(
            "sp_Role_GetById",
            new { RoleId = userDto.RoleId },
            commandType: CommandType.StoredProcedure);

        if (role is null)
            throw new Exception("Invalid role");

        if (role.Level > context.RoleLevel)
            throw new UnauthorizedAccessException("Cannot assign higher role");

        if (context.RoleLevel < 100)
        {
            if (!context.HotelId.HasValue)
                throw new UnauthorizedAccessException("Invalid hotel context");

            if (!userDto.HotelId.HasValue)
                throw new UnauthorizedAccessException("Invalid hotel assignment");

            var canAccess = await connection.ExecuteScalarAsync<int>(
                "sp_Hotel_IsInHierarchy",
                new { RootHotelId = context.HotelId.Value, TargetHotelId = userDto.HotelId.Value },
                commandType: CommandType.StoredProcedure);

            if (canAccess != 1)
                throw new UnauthorizedAccessException("Invalid hotel assignment");
        }

        var createdUser = await connection.QueryFirstOrDefaultAsync<User>(
            "sp_User_Create",
            new
            {
                UserToken = Guid.NewGuid(),
                userDto.FirstName,
                userDto.LastName,
                userDto.Email,
                userDto.UserName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                userDto.RoleId,
                userDto.HotelId,
                IsActive = true,
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
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        await using var connection = connectionFactory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@ContextRoleLevel", context.RoleLevel);
        p.Add("@RootHotelId", context.RoleLevel >= 100 ? (int?)null : context.HotelId);
        p.Add("@EffectiveUserToken", context.RoleLevel <= 10 ? context.EffectiveUserToken : (Guid?)null);
        p.Add("@SearchField", string.IsNullOrWhiteSpace(searchField) ? null : searchField.Trim().ToLower());
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var users = (await connection.QueryAsync<User>(
            "sp_User_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<UserDto>
        {
            Items = mapper.Map<List<UserDto>>(users),
            TotalCount = p.Get<int>("@TotalCount"),
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
            throw new UnauthorizedAccessException("Cannot edit higher role");

        if (context.RoleLevel < 100 && context.HotelId.HasValue)
        {
            if (!existing.HotelId.HasValue)
                throw new UnauthorizedAccessException("Cannot edit user from another hotel");

            var canAccess = await connection.ExecuteScalarAsync<int>(
                "sp_Hotel_IsInHierarchy",
                new { RootHotelId = context.HotelId.Value, TargetHotelId = existing.HotelId.Value },
                commandType: CommandType.StoredProcedure);

            if (canAccess != 1)
                throw new UnauthorizedAccessException("Cannot edit user from another hotel");
        }

        var newRoleId = existing.RoleId;
        if (request.RoleId != 0 && request.RoleId != existing.RoleId)
        {
            var newRole = await connection.QueryFirstOrDefaultAsync<Role>(
                "sp_Role_GetById",
                new { RoleId = request.RoleId },
                commandType: CommandType.StoredProcedure);

            if (newRole is null)
                throw new Exception("Invalid role");

            if (newRole.Level > context.RoleLevel)
                throw new UnauthorizedAccessException("Cannot assign higher role");

            newRoleId = newRole.RoleId;
        }

        var updatedUser = await connection.QueryFirstOrDefaultAsync<User>(
            "sp_User_Update",
            new
            {
                UserToken = request.UserToken,
                Email = !string.IsNullOrWhiteSpace(request.Email) ? request.Email : existing.Email,
                FirstName = !string.IsNullOrWhiteSpace(request.FirstName) ? request.FirstName : existing.FirstName,
                LastName = !string.IsNullOrWhiteSpace(request.LastName) ? request.LastName : existing.LastName,
                UserName = !string.IsNullOrWhiteSpace(request.UserName) ? request.UserName : existing.UserName,
                PasswordHash = !string.IsNullOrWhiteSpace(request.Password)
                    ? BCrypt.Net.BCrypt.HashPassword(request.Password)
                    : existing.PasswordHash,
                RoleId = newRoleId,
                HotelId = existing.HotelId,
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
            throw new UnauthorizedAccessException("Cannot delete higher role");

        if (context.RoleLevel < 100 && context.HotelId.HasValue)
        {
            if (!existing.HotelId.HasValue)
                throw new UnauthorizedAccessException("Cannot delete user from another hotel");

            var canAccess = await connection.ExecuteScalarAsync<int>(
                "sp_Hotel_IsInHierarchy",
                new { RootHotelId = context.HotelId.Value, TargetHotelId = existing.HotelId.Value },
                commandType: CommandType.StoredProcedure);

            if (canAccess != 1)
                throw new UnauthorizedAccessException("Cannot delete user from another hotel");
        }

        await connection.ExecuteAsync(
            "sp_User_SoftDelete",
            new { UserToken = userToken, LastUpdatedUtc = DateTime.UtcNow, LastUpdatedBy = context.ActorUserToken.ToString() },
            commandType: CommandType.StoredProcedure);

        return true;
    }

    public async Task<bool> IsUserExists(string email, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var result = await connection.ExecuteScalarAsync<int>(
            "sp_User_ExistsByEmail",
            new { Email = email },
            commandType: CommandType.StoredProcedure);

        return result == 1;
    }
}
