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

public class HotelService(IDbConnectionFactory connectionFactory, IMapper mapper) : IHotelService
{
    private sealed class HotelPageRow : Hotel { public int TotalCount { get; set; } }

    public async Task<PagedResult<HotelDto>> GetHotelsAsync(
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
        p.Add("@RootHotelId", context.RoleLevel >= 100 ? (int?)null : context.HotelId);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<HotelPageRow>(
            "sp_Hotel_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<HotelDto>
        {
            Items = mapper.MapList<HotelDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<HotelDto?> GetHotelByTokenAsync(Guid hotelToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var hotel = await connection.QueryFirstOrDefaultAsync<Hotel>(
            "sp_Hotel_GetByToken",
            new
            {
                HotelToken = hotelToken,
                RootHotelId = context.RoleLevel >= 100 ? (int?)null : context.HotelId
            },
            commandType: CommandType.StoredProcedure);

        return hotel is null ? null : mapper.Map<HotelDto>(hotel);
    }

    public async Task<bool> HotelExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var result = await connection.ExecuteScalarAsync<int>(
            "sp_Hotel_ExistsByName",
            new { NormalizedName = name.ToUpperInvariant() },
            commandType: CommandType.StoredProcedure);

        return result == 1;
    }

    public async Task<HotelDto?> CreateHotelAsync(HotelDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        if (context.RoleLevel < 100)
            throw new UnauthorizedAccessException("Only super admins can create hotels.");

        await using var connection = connectionFactory.CreateConnection();

        var created = await connection.QueryFirstOrDefaultAsync<Hotel>(
            "sp_Hotel_Create",
            new
            {
                HotelToken = Guid.NewGuid(),
                Name = dto.Name,
                NormalizedName = dto.Name.ToUpperInvariant(),
                LegalName = dto.LegalName,
                Code = dto.Code,
                ParentHotelId = dto.ParentHotelId,
                TimeZone = dto.TimeZone,
                CurrencyCode = dto.CurrencyCode,
                LanguageCode = dto.LanguageCode,
                IsActive = true,
                IsDeleted = false,
                CreatedUtc = DateTime.UtcNow,
                CreatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return created is null ? null : mapper.Map<HotelDto>(created);
    }

    public async Task<HotelDto?> EditHotelAsync(HotelDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Hotel>(
            "sp_Hotel_GetByToken",
            new { HotelToken = dto.HotelToken, RootHotelId = (int?)null },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        if (context.RoleLevel < 100 && context.HotelId != existing.HotelId)
            throw new UnauthorizedAccessException("Cannot edit a hotel outside your scope.");

        var newName = !string.IsNullOrWhiteSpace(dto.Name) ? dto.Name : existing.Name;

        var updated = await connection.QueryFirstOrDefaultAsync<Hotel>(
            "sp_Hotel_Update",
            new
            {
                HotelToken = dto.HotelToken,
                Name = newName,
                NormalizedName = newName.ToUpperInvariant(),
                LegalName = dto.LegalName ?? existing.LegalName,
                Code = dto.Code ?? existing.Code,
                ParentHotelId = dto.ParentHotelId ?? existing.ParentHotelId,
                TimeZone = dto.TimeZone ?? existing.TimeZone,
                CurrencyCode = dto.CurrencyCode ?? existing.CurrencyCode,
                LanguageCode = dto.LanguageCode ?? existing.LanguageCode,
                LastUpdatedUtc = DateTime.UtcNow,
                LastUpdatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<HotelDto>(updated);
    }

    public async Task<bool> DeleteHotelAsync(Guid hotelToken, IRequestContext context, CancellationToken cancellationToken)
    {
        if (context.RoleLevel < 100)
            throw new UnauthorizedAccessException("Only super admins can delete hotels.");

        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Hotel>(
            "sp_Hotel_GetByToken",
            new { HotelToken = hotelToken, RootHotelId = (int?)null },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return false;

        await connection.ExecuteAsync(
            "sp_Hotel_SoftDelete",
            new
            {
                HotelToken = hotelToken,
                DeletedUtc = DateTime.UtcNow,
                DeletedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return true;
    }
}
