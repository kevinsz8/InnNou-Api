using AutoMapper;
using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
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
            Items = mapper.Map<List<HotelDto>>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }
}
