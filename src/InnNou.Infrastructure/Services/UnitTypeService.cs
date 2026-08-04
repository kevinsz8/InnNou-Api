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

public class UnitTypeService(IDbConnectionFactory connectionFactory, IMapper mapper) : IUnitTypeService
{
    private sealed class UnitTypePageRow : UnitType { public int TotalCount { get; set; } }

    // UnitType is a pure global catalog (no per-organization ownership) — a flat AdminRoleLevel
    // gate on every write method is the whole authorization model, same shape as FamilyService.
    private const int AdminRoleLevel = 80;
    private const int MaxPageSize = 100;

    public async Task<PagedResult<UnitTypeDto>> GetPagedAsync(int pageNumber, int pageSize, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@IncludeInactive", includeInactive);
        var rows = (await connection.QueryAsync<UnitTypePageRow>(
            "sp_UnitType_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();
        return new PagedResult<UnitTypeDto>
        {
            Items = mapper.MapList<UnitTypeDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<UnitTypeDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitTypeToken", token);
        var row = await connection.QueryFirstOrDefaultAsync<UnitType>(
            "sp_UnitType_GetByToken", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitTypeDto>(row);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", code);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_UnitType_ExistsByCode", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<UnitTypeDto?> CreateAsync(UnitTypeDto dto, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.UnitTypeForbidden, "Only Admins and SuperAdmins can create unit types.", 403);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitTypeToken", Guid.NewGuid());
        p.Add("@Code", dto.Code);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<UnitType>(
            "sp_UnitType_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitTypeDto>(row);
    }

    public async Task<UnitTypeDto?> EditAsync(UnitTypeDto dto, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.UnitTypeForbidden, "Only Admins and SuperAdmins can edit unit types.", 403);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitTypeToken", dto.UnitTypeToken);
        p.Add("@Code", dto.Code);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<UnitType>(
            "sp_UnitType_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<UnitTypeDto>(row);
    }

    public async Task<UnitTypeDto?> SetActiveAsync(Guid token, bool isActive, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.UnitTypeForbidden, "Only Admins and SuperAdmins can activate/deactivate unit types.", 403);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitTypeToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        try
        {
            var row = await connection.QueryFirstOrDefaultAsync<UnitType>(
                "sp_UnitType_SetActive", p, commandType: CommandType.StoredProcedure);
            return row is null ? null : mapper.Map<UnitTypeDto>(row);
        }
        catch (SqlException ex) when (ex.Message.Contains("UNIT_TYPE_SYSTEM_READONLY", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ErrorCodes.UnitTypeSystemReadonly, "A system-defined unit type cannot be deactivated.", 400);
        }
    }

    public async Task<UnitTypeDto?> SetNameTranslationsAsync(Guid unitTypeToken, Dictionary<string, string> translations, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.UnitTypeForbidden, "Only Admins and SuperAdmins can edit a unit type's name translations.", 403);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@UnitTypeToken", unitTypeToken);
        p.Add("@NameTranslations", translations.Count == 0 ? null : JsonSerializer.Serialize(translations));
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        try
        {
            var row = await connection.QueryFirstOrDefaultAsync<UnitType>(
                "sp_UnitType_SetNameTranslations", p, commandType: CommandType.StoredProcedure);
            return row is null ? null : mapper.Map<UnitTypeDto>(row);
        }
        catch (SqlException ex) when (ex.Message.Contains("INVALID_REQUEST", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ErrorCodes.InvalidRequest, "Invalid name translations payload.", 400);
        }
    }
}
