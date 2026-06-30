using Dapper;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class UnitConversionRateService(IDbConnectionFactory connectionFactory, IMapper mapper) : IUnitConversionRateService
{
    private sealed class UnitConversionRatePageRow : UnitConversionRate { public int TotalCount { get; set; } }

    public async Task<PagedResult<UnitConversionRateDto>> GetPagedAsync(int pageNumber, int pageSize, int? unitTypeId = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@UnitTypeId", unitTypeId);
        p.Add("@IncludeInactive", includeInactive);
        var rows = (await connection.QueryAsync<UnitConversionRatePageRow>(
            "sp_UnitConversionRate_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();
        return new PagedResult<UnitConversionRateDto>
        {
            Items = mapper.MapList<UnitConversionRateDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<UnitConversionRateDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitConversionRateToken", token);
        var row = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
            "sp_UnitConversionRate_GetByToken", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitConversionRateDto>(row);
    }

    public async Task<UnitConversionRateDto?> CreateAsync(UnitConversionRateDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var forwardToken = Guid.NewGuid();
        var p = new DynamicParameters();
        p.Add("@UnitConversionRateToken", forwardToken);
        p.Add("@FromUnitOfMeasureId", dto.FromUnitOfMeasureId);
        p.Add("@ToUnitOfMeasureId", dto.ToUnitOfMeasureId);
        p.Add("@Factor", dto.Factor);
        p.Add("@CreatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
            "sp_UnitConversionRate_Create", p, commandType: CommandType.StoredProcedure);
        if (row is null) return null;

        // Create reverse pair transparently; skip if it already exists
        try
        {
            var rp = new DynamicParameters();
            rp.Add("@UnitConversionRateToken", Guid.NewGuid());
            rp.Add("@FromUnitOfMeasureId", dto.ToUnitOfMeasureId);
            rp.Add("@ToUnitOfMeasureId", dto.FromUnitOfMeasureId);
            rp.Add("@Factor", dto.Factor == 0 ? 0 : 1m / dto.Factor);
            rp.Add("@CreatedBy", "API");
            await connection.ExecuteAsync("sp_UnitConversionRate_Create", rp, commandType: CommandType.StoredProcedure);
        }
        catch { /* reverse pair already exists — no-op */ }

        return mapper.Map<UnitConversionRateDto>(row);
    }

    public async Task<UnitConversionRateDto?> EditAsync(UnitConversionRateDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@UnitConversionRateToken", dto.UnitConversionRateToken);
        p.Add("@Factor", dto.Factor);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
            "sp_UnitConversionRate_Update", p, commandType: CommandType.StoredProcedure);
        if (row is null) return null;

        // Update reverse pair
        var rp = new DynamicParameters();
        rp.Add("@FromUnitOfMeasureId", row.ToUnitOfMeasureId);
        rp.Add("@ToUnitOfMeasureId", row.FromUnitOfMeasureId);
        var reverseRow = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
            "sp_UnitConversionRate_GetByPair", rp, commandType: CommandType.StoredProcedure);
        if (reverseRow is not null)
        {
            var rrp = new DynamicParameters();
            rrp.Add("@UnitConversionRateToken", reverseRow.UnitConversionRateToken);
            rrp.Add("@Factor", dto.Factor == 0 ? 0 : 1m / dto.Factor);
            rrp.Add("@LastUpdatedBy", "API");
            await connection.ExecuteAsync("sp_UnitConversionRate_Update", rrp, commandType: CommandType.StoredProcedure);
        }

        return mapper.Map<UnitConversionRateDto>(row);
    }

    public async Task<UnitConversionRateDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@UnitConversionRateToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
            "sp_UnitConversionRate_SetActive", p, commandType: CommandType.StoredProcedure);
        if (row is null) return null;

        // Mirror active state on reverse pair
        var rp = new DynamicParameters();
        rp.Add("@FromUnitOfMeasureId", row.ToUnitOfMeasureId);
        rp.Add("@ToUnitOfMeasureId", row.FromUnitOfMeasureId);
        var reverseRow = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
            "sp_UnitConversionRate_GetByPair", rp, commandType: CommandType.StoredProcedure);
        if (reverseRow is not null)
        {
            var rrp = new DynamicParameters();
            rrp.Add("@UnitConversionRateToken", reverseRow.UnitConversionRateToken);
            rrp.Add("@IsActive", isActive);
            rrp.Add("@LastUpdatedBy", "API");
            await connection.ExecuteAsync("sp_UnitConversionRate_SetActive", rrp, commandType: CommandType.StoredProcedure);
        }

        return mapper.Map<UnitConversionRateDto>(row);
    }

    public async Task<bool> DeleteAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@UnitConversionRateToken", token);
        var deleted = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
            "sp_UnitConversionRate_Delete", p, commandType: CommandType.StoredProcedure);
        if (deleted is null) return false;

        // Delete reverse pair
        var rp = new DynamicParameters();
        rp.Add("@FromUnitOfMeasureId", deleted.ToUnitOfMeasureId);
        rp.Add("@ToUnitOfMeasureId", deleted.FromUnitOfMeasureId);
        var reverseRow = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
            "sp_UnitConversionRate_GetByPair", rp, commandType: CommandType.StoredProcedure);
        if (reverseRow is not null)
        {
            var rrp = new DynamicParameters();
            rrp.Add("@UnitConversionRateToken", reverseRow.UnitConversionRateToken);
            await connection.ExecuteAsync("sp_UnitConversionRate_Delete", rrp, commandType: CommandType.StoredProcedure);
        }

        return true;
    }
}
