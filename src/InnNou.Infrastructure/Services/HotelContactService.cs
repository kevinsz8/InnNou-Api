using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class HotelContactService(IDbConnectionFactory connectionFactory, IMapper mapper) : IHotelContactService
{
    private sealed class HotelContactPageRow : HotelContact { public int TotalCount { get; set; } }

    public async Task<PagedResult<HotelContactDto>> GetPagedByHotelTokenAsync(
        Guid hotelToken,
        int pageNumber,
        int pageSize,
        string? searchText,
        bool includeInactive,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        await using var connection = connectionFactory.CreateConnection();

        var hotel = await connection.QueryFirstOrDefaultAsync<Hotel>(
            "sp_Hotel_GetByToken",
            new { HotelToken = hotelToken, RootHotelId = context.RoleLevel >= 100 ? (int?)null : context.HotelId },
            commandType: CommandType.StoredProcedure);

        if (hotel is null)
            return new PagedResult<HotelContactDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

        var p = new DynamicParameters();
        p.Add("@HotelId", hotel.HotelId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@IncludeInactive", includeInactive);

        var rows = (await connection.QueryAsync<HotelContactPageRow>(
            "sp_HotelContact_GetPagedByHotelId", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<HotelContactDto>
        {
            Items = mapper.MapList<HotelContactDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<HotelContactDto?> GetByTokenAsync(Guid hotelContactToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var contact = await connection.QueryFirstOrDefaultAsync<HotelContact>(
            "sp_HotelContact_GetByToken",
            new { HotelContactToken = hotelContactToken },
            commandType: CommandType.StoredProcedure);

        if (contact is null)
            return null;

        if (context.RoleLevel < 100 && context.HotelId.HasValue)
        {
            var canAccess = await connection.ExecuteScalarAsync<int>(
                "sp_Hotel_IsInHierarchy",
                new { RootHotelId = context.HotelId.Value, TargetHotelId = contact.HotelId },
                commandType: CommandType.StoredProcedure);

            if (canAccess != 1)
                return null;
        }

        return mapper.Map<HotelContactDto>(contact);
    }

    public async Task<HotelContactDto?> CreateAsync(HotelContactDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var hotel = await connection.QueryFirstOrDefaultAsync<Hotel>(
            "sp_Hotel_GetByToken",
            new { HotelToken = dto.HotelToken, RootHotelId = context.RoleLevel >= 100 ? (int?)null : context.HotelId },
            commandType: CommandType.StoredProcedure);

        if (hotel is null)
            return null;

        var p = new DynamicParameters();
        p.Add("@HotelContactToken", Guid.NewGuid());
        p.Add("@HotelId", hotel.HotelId);
        p.Add("@ContactName", dto.ContactName);
        p.Add("@ContactType", dto.ContactType);
        p.Add("@Department", dto.Department);
        p.Add("@Phone", dto.Phone);
        p.Add("@Mobile", dto.Mobile);
        p.Add("@Fax", dto.Fax);
        p.Add("@Email", dto.Email);
        p.Add("@Notes", dto.Notes);
        p.Add("@IsPrimary", dto.IsPrimary);
        p.Add("@CreatedUtc", DateTime.UtcNow);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());

        var created = await connection.QueryFirstOrDefaultAsync<HotelContact>(
            "sp_HotelContact_Create", p, commandType: CommandType.StoredProcedure);

        return created is null ? null : mapper.Map<HotelContactDto>(created);
    }

    public async Task<HotelContactDto?> EditAsync(HotelContactDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<HotelContact>(
            "sp_HotelContact_GetByToken",
            new { HotelContactToken = dto.HotelContactToken },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        if (context.RoleLevel < 100 && context.HotelId.HasValue)
        {
            var canAccess = await connection.ExecuteScalarAsync<int>(
                "sp_Hotel_IsInHierarchy",
                new { RootHotelId = context.HotelId.Value, TargetHotelId = existing.HotelId },
                commandType: CommandType.StoredProcedure);

            if (canAccess != 1)
                throw new UnauthorizedAccessException("Cannot edit contact from another hotel.");
        }

        var p = new DynamicParameters();
        p.Add("@HotelContactToken", dto.HotelContactToken);
        p.Add("@ContactName", dto.ContactName);
        p.Add("@ContactType", dto.ContactType);
        p.Add("@Department", dto.Department);
        p.Add("@Phone", dto.Phone);
        p.Add("@Mobile", dto.Mobile);
        p.Add("@Fax", dto.Fax);
        p.Add("@Email", dto.Email);
        p.Add("@Notes", dto.Notes);
        p.Add("@IsPrimary", dto.IsPrimary);
        p.Add("@LastUpdatedUtc", DateTime.UtcNow);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());

        var updated = await connection.QueryFirstOrDefaultAsync<HotelContact>(
            "sp_HotelContact_Update", p, commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<HotelContactDto>(updated);
    }

    public async Task<bool> DeleteAsync(Guid hotelContactToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<HotelContact>(
            "sp_HotelContact_GetByToken",
            new { HotelContactToken = hotelContactToken },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return false;

        if (context.RoleLevel < 100 && context.HotelId.HasValue)
        {
            var canAccess = await connection.ExecuteScalarAsync<int>(
                "sp_Hotel_IsInHierarchy",
                new { RootHotelId = context.HotelId.Value, TargetHotelId = existing.HotelId },
                commandType: CommandType.StoredProcedure);

            if (canAccess != 1)
                throw new UnauthorizedAccessException("Cannot delete contact from another hotel.");
        }

        var now = DateTime.UtcNow;
        var actor = context.ActorUserToken.ToString();

        await connection.ExecuteAsync(
            "sp_HotelContact_SoftDelete",
            new
            {
                HotelContactToken = hotelContactToken,
                DeletedUtc = now,
                DeletedBy = actor,
                LastUpdatedUtc = now,
                LastUpdatedBy = actor
            },
            commandType: CommandType.StoredProcedure);

        return true;
    }
}
