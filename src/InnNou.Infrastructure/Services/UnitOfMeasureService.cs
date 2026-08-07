using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace InnNou.Infrastructure.Services;

public class UnitOfMeasureService(IDbConnectionFactory connectionFactory, IMapper mapper) : IUnitOfMeasureService
{
    private sealed class UnitOfMeasurePageRow : UnitOfMeasure { public int TotalCount { get; set; } }

    // UnitOfMeasure is a pure global catalog (no per-organization ownership) — a flat
    // AdminRoleLevel gate on every write method is the whole authorization model.
    private const int AdminRoleLevel = 80;
    private const int MaxPageSize = 100;

    public async Task<PagedResult<UnitOfMeasureDto>> GetPagedAsync(int pageNumber, int pageSize, int? unitTypeId = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@UnitTypeId", unitTypeId);
        p.Add("@IncludeInactive", includeInactive);
        var rows = (await connection.QueryAsync<UnitOfMeasurePageRow>(
            "sp_UnitOfMeasure_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();
        return new PagedResult<UnitOfMeasureDto>
        {
            Items = mapper.MapList<UnitOfMeasureDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<UnitOfMeasureDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitOfMeasureToken", token);
        var row = await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
            "sp_UnitOfMeasure_GetByToken", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitOfMeasureDto>(row);
    }

    public async Task<bool> ExistsByCodeAsync(string code, int unitTypeId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", code);
        p.Add("@UnitTypeId", unitTypeId);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_UnitOfMeasure_ExistsByCode", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<UnitOfMeasureDto?> CreateAsync(UnitOfMeasureDto dto, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.UnitOfMeasureForbidden, "Only Admins and SuperAdmins can create units of measure.", 403);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitOfMeasureToken", Guid.NewGuid());
        p.Add("@UnitTypeId", dto.UnitTypeId);
        p.Add("@Code", dto.Code);
        p.Add("@Symbol", dto.Symbol);
        p.Add("@Decimals", dto.Decimals);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
            "sp_UnitOfMeasure_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitOfMeasureDto>(row);
    }

    public async Task<UnitOfMeasureDto?> EditAsync(UnitOfMeasureDto dto, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.UnitOfMeasureForbidden, "Only Admins and SuperAdmins can edit units of measure.", 403);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitOfMeasureToken", dto.UnitOfMeasureToken);
        p.Add("@Code", dto.Code);
        p.Add("@Symbol", dto.Symbol);
        p.Add("@Decimals", dto.Decimals);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        try
        {
            var row = await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
                "sp_UnitOfMeasure_Update", p, commandType: CommandType.StoredProcedure);
            return row is null ? null : mapper.Map<UnitOfMeasureDto>(row);
        }
        catch (SqlException ex) when (ex.Message.Contains("UNIT_OF_MEASURE_CODE_EXISTS", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ErrorCodes.UnitOfMeasureCodeExists, "A unit of measure with this code already exists.", 409);
        }
    }

    public async Task<UnitOfMeasureDto?> SetActiveAsync(Guid token, bool isActive, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.UnitOfMeasureForbidden, "Only Admins and SuperAdmins can activate/deactivate units of measure.", 403);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitOfMeasureToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        try
        {
            var row = await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
                "sp_UnitOfMeasure_SetActive", p, commandType: CommandType.StoredProcedure);
            return row is null ? null : mapper.Map<UnitOfMeasureDto>(row);
        }
        catch (SqlException ex) when (ex.Message.Contains("UNIT_OF_MEASURE_SYSTEM_READONLY", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ErrorCodes.UnitOfMeasureSystemReadonly, "A system-defined unit of measure cannot be deactivated.", 400);
        }
    }

    public async Task<UnitOfMeasureDto?> SetNameTranslationsAsync(Guid unitOfMeasureToken, Dictionary<string, string> translations, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.UnitOfMeasureForbidden, "Only Admins and SuperAdmins can edit a unit of measure's name translations.", 403);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitOfMeasureToken", unitOfMeasureToken);
        p.Add("@NameTranslations", translations.Count == 0 ? null : JsonSerializer.Serialize(translations));
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        try
        {
            var row = await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
                "sp_UnitOfMeasure_SetNameTranslations", p, commandType: CommandType.StoredProcedure);
            return row is null ? null : mapper.Map<UnitOfMeasureDto>(row);
        }
        catch (SqlException ex) when (ex.Message.Contains("INVALID_REQUEST", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ErrorCodes.InvalidRequest, "Invalid name translations payload.", 400);
        }
    }
}
