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

namespace InnNou.Infrastructure.Services;

public class UnitConversionRateService(IDbConnectionFactory connectionFactory, IMapper mapper) : IUnitConversionRateService
{
    private sealed class UnitConversionRatePageRow : UnitConversionRate { public int TotalCount { get; set; } }

    // Pure global catalog (no per-organization ownership) — a flat AdminRoleLevel gate on every
    // write method, including Delete, is the whole authorization model. Delete is a real physical
    // row delete (no soft-delete/IsActive recovery), so gating it is the highest-severity part of
    // this fix.
    private const int AdminRoleLevel = 80;
    private const int MaxPageSize = 100;

    public async Task<PagedResult<UnitConversionRateDto>> GetPagedAsync(int pageNumber, int pageSize, int? unitTypeId = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

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

    public async Task<UnitConversionRateDto?> CreateAsync(UnitConversionRateDto dto, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.UnitConversionRateForbidden, "Only Admins and SuperAdmins can create unit conversion rates.", 403);

        await using var connection = connectionFactory.CreateConnection();

        var forwardToken = Guid.NewGuid();
        var p = new DynamicParameters();
        p.Add("@UnitConversionRateToken", forwardToken);
        p.Add("@FromUnitOfMeasureId", dto.FromUnitOfMeasureId);
        p.Add("@ToUnitOfMeasureId", dto.ToUnitOfMeasureId);
        p.Add("@Factor", dto.Factor);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());
        UnitConversionRate? row;
        try
        {
            row = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
                "sp_UnitConversionRate_Create", p, commandType: CommandType.StoredProcedure);
        }
        catch (SqlException ex) when (ex.Message.Contains("FROM_UNIT_NOT_FOUND", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ErrorCodes.UnitConversionRateFromUnitNotFound, "The source unit of measure was not found or is inactive.", 404);
        }
        catch (SqlException ex) when (ex.Message.Contains("TO_UNIT_NOT_FOUND", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ErrorCodes.UnitConversionRateToUnitNotFound, "The target unit of measure was not found or is inactive.", 404);
        }
        catch (SqlException ex) when (ex.Message.Contains("CONVERSION_CROSS_TYPE_INVALID", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ErrorCodes.UnitConversionRateCrossTypeInvalid, "Cannot create a conversion between units of different unit types.", 400);
        }
        catch (SqlException ex) when (ex.Message.Contains("CONVERSION_COUNT_TYPE_NOT_ALLOWED", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ErrorCodes.UnitConversionRateCountTypeNotAllowed, "Count-type units (Box, Bottle, Pack...) have no universal conversion factor — this ratio is specific to each article's own packaging and is configured on the article itself.", 400);
        }
        catch (SqlException ex) when (ex.Message.Contains("CONVERSION_SAME_UNIT_INVALID", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ErrorCodes.UnitConversionRateSameUnitInvalid, "A unit cannot be converted to itself.", 400);
        }
        catch (SqlException ex) when (ex.Message.Contains("CONVERSION_PAIR_EXISTS", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ErrorCodes.UnitConversionRatePairExists, "A conversion rate for this pair of units already exists.", 409);
        }
        if (row is null) return null;

        // Create reverse pair transparently; skip only if it already exists (CONVERSION_PAIR_EXISTS) —
        // any other failure (timeout, deadlock, etc.) must propagate rather than be silently swallowed,
        // since that would leave the forward row committed with no reverse counterpart and no error surfaced.
        try
        {
            var rp = new DynamicParameters();
            rp.Add("@UnitConversionRateToken", Guid.NewGuid());
            rp.Add("@FromUnitOfMeasureId", dto.ToUnitOfMeasureId);
            rp.Add("@ToUnitOfMeasureId", dto.FromUnitOfMeasureId);
            rp.Add("@Factor", dto.Factor == 0 ? 0 : 1m / dto.Factor);
            rp.Add("@CreatedBy", context.ActorUserToken.ToString());
            await connection.ExecuteAsync("sp_UnitConversionRate_Create", rp, commandType: CommandType.StoredProcedure);
        }
        catch (SqlException ex) when (ex.Message.Contains("CONVERSION_PAIR_EXISTS", StringComparison.OrdinalIgnoreCase))
        {
            /* reverse pair already exists — no-op */
        }

        return mapper.Map<UnitConversionRateDto>(row);
    }

    public async Task<UnitConversionRateDto?> EditAsync(UnitConversionRateDto dto, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.UnitConversionRateForbidden, "Only Admins and SuperAdmins can edit unit conversion rates.", 403);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var p = new DynamicParameters();
            p.Add("@UnitConversionRateToken", dto.UnitConversionRateToken);
            p.Add("@Factor", dto.Factor);
            p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
            var row = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
                "sp_UnitConversionRate_Update", p, transaction, commandType: CommandType.StoredProcedure);
            if (row is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            // Update reverse pair, in the same transaction — a failure here must not leave the
            // forward row updated with a stale reverse counterpart.
            var rp = new DynamicParameters();
            rp.Add("@FromUnitOfMeasureId", row.ToUnitOfMeasureId);
            rp.Add("@ToUnitOfMeasureId", row.FromUnitOfMeasureId);
            var reverseRow = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
                "sp_UnitConversionRate_GetByPair", rp, transaction, commandType: CommandType.StoredProcedure);
            if (reverseRow is not null)
            {
                var rrp = new DynamicParameters();
                rrp.Add("@UnitConversionRateToken", reverseRow.UnitConversionRateToken);
                rrp.Add("@Factor", dto.Factor == 0 ? 0 : 1m / dto.Factor);
                rrp.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
                await connection.ExecuteAsync("sp_UnitConversionRate_Update", rrp, transaction, commandType: CommandType.StoredProcedure);
            }

            await transaction.CommitAsync(cancellationToken);
            return mapper.Map<UnitConversionRateDto>(row);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<UnitConversionRateDto?> SetActiveAsync(Guid token, bool isActive, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.UnitConversionRateForbidden, "Only Admins and SuperAdmins can activate/deactivate unit conversion rates.", 403);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var p = new DynamicParameters();
            p.Add("@UnitConversionRateToken", token);
            p.Add("@IsActive", isActive);
            p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
            var row = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
                "sp_UnitConversionRate_SetActive", p, transaction, commandType: CommandType.StoredProcedure);
            if (row is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            // Mirror active state on reverse pair, in the same transaction — a failure here must not
            // leave the forward row's active state out of sync with its reverse counterpart.
            var rp = new DynamicParameters();
            rp.Add("@FromUnitOfMeasureId", row.ToUnitOfMeasureId);
            rp.Add("@ToUnitOfMeasureId", row.FromUnitOfMeasureId);
            var reverseRow = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
                "sp_UnitConversionRate_GetByPair", rp, transaction, commandType: CommandType.StoredProcedure);
            if (reverseRow is not null)
            {
                var rrp = new DynamicParameters();
                rrp.Add("@UnitConversionRateToken", reverseRow.UnitConversionRateToken);
                rrp.Add("@IsActive", isActive);
                rrp.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
                await connection.ExecuteAsync("sp_UnitConversionRate_SetActive", rrp, transaction, commandType: CommandType.StoredProcedure);
            }

            await transaction.CommitAsync(cancellationToken);
            return mapper.Map<UnitConversionRateDto>(row);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid token, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.UnitConversionRateForbidden, "Only Admins and SuperAdmins can delete unit conversion rates.", 403);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var p = new DynamicParameters();
            p.Add("@UnitConversionRateToken", token);
            var deleted = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
                "sp_UnitConversionRate_Delete", p, transaction, commandType: CommandType.StoredProcedure);
            if (deleted is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            // Delete reverse pair, in the same transaction — a failure here must not leave the
            // forward row deleted with an orphaned reverse counterpart still present.
            var rp = new DynamicParameters();
            rp.Add("@FromUnitOfMeasureId", deleted.ToUnitOfMeasureId);
            rp.Add("@ToUnitOfMeasureId", deleted.FromUnitOfMeasureId);
            var reverseRow = await connection.QueryFirstOrDefaultAsync<UnitConversionRate>(
                "sp_UnitConversionRate_GetByPair", rp, transaction, commandType: CommandType.StoredProcedure);
            if (reverseRow is not null)
            {
                var rrp = new DynamicParameters();
                rrp.Add("@UnitConversionRateToken", reverseRow.UnitConversionRateToken);
                await connection.ExecuteAsync("sp_UnitConversionRate_Delete", rrp, transaction, commandType: CommandType.StoredProcedure);
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
