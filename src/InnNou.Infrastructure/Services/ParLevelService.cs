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

public class ParLevelService(IDbConnectionFactory connectionFactory, IMapper mapper) : IParLevelService
{
    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    // Read visibility, no OrganizationTypeCode restriction — mirrors InventoryService's own copy.
    private static async Task<bool> CanReadOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId, int? targetWarehouseId = null)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return true;

        if (!context.OrganizationId.HasValue)
            return false;

        if (!WarehouseScopeGuard.Allows(context, targetWarehouseId))
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    // Write visibility — mirrors InventoryService's own copy: only a caller whose own organization
    // is ASSOCIATE may write; SuperAdmin (no organization of their own) and SUPER_ASSOCIATE are
    // read-only — par-level configuration happens at the property level, same reasoning as
    // Orders/Goods Receipts/Inventory.
    private static async Task<bool> CanManageOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId, int? targetWarehouseId = null)
    {
        if (context.OrganizationTypeCode != OrganizationTypeCodes.Associate)
            return false;

        if (context.RoleLevel < StaffRoleLevel || !context.OrganizationId.HasValue)
            return false;

        if (!WarehouseScopeGuard.Allows(context, targetWarehouseId))
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    private static int ToMmdd(int month, int day) => month * 100 + day;

    // A SEASONAL window can wrap across the year boundary (e.g. Dec 20 -> Jan 6). Decomposed into
    // 1 or 2 non-wrapping MMDD sub-ranges so overlap can be checked uniformly against another
    // window's own sub-ranges.
    private static List<(int Start, int End)> DecomposeSeasonalRanges(int startMmdd, int endMmdd) =>
        startMmdd <= endMmdd
            ? [(startMmdd, endMmdd)]
            : [(startMmdd, 1231), (101, endMmdd)];

    private static bool RangesOverlap(int aStart, int aEnd, int bStart, int bEnd) => aStart <= bEnd && bStart <= aEnd;

    // Shared by CreateOverrideAsync's fast-path pre-check and its DB-lock-protected backstop
    // re-check (see the TOCTOU-race comment in CreateOverrideAsync) so the two can never drift.
    private static bool TryFindOverlap(
        ParLevelOverrideType type, List<ParLevelOverride> sameTypeOverrides,
        int? startMonth, int? startDay, int? endMonth, int? endDay,
        DateOnly? startDate, DateOnly? endDate, out string? conflictLabel)
    {
        if (type == ParLevelOverrideType.Seasonal)
        {
            var newRanges = DecomposeSeasonalRanges(ToMmdd(startMonth!.Value, startDay!.Value), ToMmdd(endMonth!.Value, endDay!.Value));

            foreach (var existing in sameTypeOverrides)
            {
                var existingRanges = DecomposeSeasonalRanges(
                    ToMmdd(existing.StartMonth!.Value, existing.StartDay!.Value),
                    ToMmdd(existing.EndMonth!.Value, existing.EndDay!.Value));

                if (newRanges.Any(nr => existingRanges.Any(er => RangesOverlap(nr.Start, nr.End, er.Start, er.End))))
                {
                    conflictLabel = existing.Label ?? "unnamed";
                    return true;
                }
            }
        }
        else
        {
            foreach (var existing in sameTypeOverrides)
            {
                var existingStart = DateOnly.FromDateTime(existing.StartDate!.Value);
                var existingEnd = DateOnly.FromDateTime(existing.EndDate!.Value);

                if (existingStart <= endDate!.Value && startDate!.Value <= existingEnd)
                {
                    conflictLabel = existing.Label ?? "unnamed";
                    return true;
                }
            }
        }

        conflictLabel = null;
        return false;
    }

    public async Task<ParLevelDto?> CreateBaseAsync(Guid warehouseToken, Guid articleToken, decimal minimumQuantity, decimal reorderQuantity, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken }, commandType: CommandType.StoredProcedure);
        if (warehouse is null)
            throw new ApiException(ErrorCodes.ParLevelWarehouseNotFound, "Warehouse not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, warehouse.OrganizationId, warehouse.WarehouseId))
            throw new ApiException(ErrorCodes.ParLevelForbidden, "Cannot configure par levels for a warehouse outside your scope.", 403);

        var article = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = articleToken }, commandType: CommandType.StoredProcedure);
        if (article is null)
            throw new ApiException(ErrorCodes.ParLevelArticleNotFound, "Article not found.", 404);

        if (minimumQuantity < 0)
            throw new ApiException(ErrorCodes.ParLevelInvalidQuantity, "Minimum quantity cannot be negative.", 400);
        if (reorderQuantity <= 0)
            throw new ApiException(ErrorCodes.ParLevelInvalidQuantity, "Reorder quantity must be greater than zero.", 400);

        var existing = await connection.QueryFirstOrDefaultAsync<ParLevel>(
            "sp_ParLevel_GetByWarehouseAndArticle", new { warehouse.WarehouseId, article.ArticleId }, commandType: CommandType.StoredProcedure);
        if (existing is not null)
            throw new ApiException(ErrorCodes.ParLevelAlreadyExists, $"A par level is already configured for '{article.Name}' at this warehouse — edit it instead.", 400);

        var created = await connection.QueryFirstOrDefaultAsync<ParLevel>(
            "sp_ParLevel_Create",
            new
            {
                ParLevelToken = Guid.NewGuid(),
                warehouse.WarehouseId,
                article.ArticleId,
                MinimumQuantity = minimumQuantity,
                ReorderQuantity = reorderQuantity,
                CreatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return created is null ? null : mapper.Map<ParLevelDto>(created);
    }

    public async Task<ParLevelDto?> EditBaseAsync(Guid parLevelToken, decimal minimumQuantity, decimal reorderQuantity, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<ParLevel>(
            "sp_ParLevel_GetByToken", new { ParLevelToken = parLevelToken }, commandType: CommandType.StoredProcedure);
        if (existing is null)
            throw new ApiException(ErrorCodes.ParLevelNotFound, "Par level not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId, existing.WarehouseId))
            throw new ApiException(ErrorCodes.ParLevelForbidden, "Cannot edit a par level outside your scope.", 403);

        if (minimumQuantity < 0)
            throw new ApiException(ErrorCodes.ParLevelInvalidQuantity, "Minimum quantity cannot be negative.", 400);
        if (reorderQuantity <= 0)
            throw new ApiException(ErrorCodes.ParLevelInvalidQuantity, "Reorder quantity must be greater than zero.", 400);

        var updated = await connection.QueryFirstOrDefaultAsync<ParLevel>(
            "sp_ParLevel_Edit",
            new
            {
                ParLevelToken = parLevelToken,
                MinimumQuantity = minimumQuantity,
                ReorderQuantity = reorderQuantity,
                LastUpdatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<ParLevelDto>(updated);
    }

    public async Task<bool> DeleteBaseAsync(Guid parLevelToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<ParLevel>(
            "sp_ParLevel_GetByToken", new { ParLevelToken = parLevelToken }, commandType: CommandType.StoredProcedure);
        if (existing is null)
            throw new ApiException(ErrorCodes.ParLevelNotFound, "Par level not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId, existing.WarehouseId))
            throw new ApiException(ErrorCodes.ParLevelForbidden, "Cannot delete a par level outside your scope.", 403);

        // sp_ParLevel_Delete also deletes any overrides for the same (Warehouse, Article) — an
        // override is meaningless without the base row it refines.
        var deleted = await connection.QueryFirstOrDefaultAsync<bool>(
            "sp_ParLevel_Delete", new { ParLevelToken = parLevelToken }, commandType: CommandType.StoredProcedure);

        return deleted;
    }

    public async Task<ParLevelOverrideDto?> CreateOverrideAsync(
        Guid warehouseToken, Guid articleToken, ParLevelOverrideType type, string? label,
        decimal minimumQuantity, decimal reorderQuantity,
        int? startMonth, int? startDay, int? endMonth, int? endDay,
        DateOnly? startDate, DateOnly? endDate,
        IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken }, commandType: CommandType.StoredProcedure);
        if (warehouse is null)
            throw new ApiException(ErrorCodes.ParLevelWarehouseNotFound, "Warehouse not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, warehouse.OrganizationId, warehouse.WarehouseId))
            throw new ApiException(ErrorCodes.ParLevelForbidden, "Cannot configure par levels for a warehouse outside your scope.", 403);

        var article = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = articleToken }, commandType: CommandType.StoredProcedure);
        if (article is null)
            throw new ApiException(ErrorCodes.ParLevelArticleNotFound, "Article not found.", 404);

        if (minimumQuantity < 0)
            throw new ApiException(ErrorCodes.ParLevelInvalidQuantity, "Minimum quantity cannot be negative.", 400);
        if (reorderQuantity <= 0)
            throw new ApiException(ErrorCodes.ParLevelInvalidQuantity, "Reorder quantity must be greater than zero.", 400);

        // A base par level must exist before any override can refine it — keeps the resolution
        // query simple (it's always driven FROM ParLevels).
        var baseLevel = await connection.QueryFirstOrDefaultAsync<ParLevel>(
            "sp_ParLevel_GetByWarehouseAndArticle", new { warehouse.WarehouseId, article.ArticleId }, commandType: CommandType.StoredProcedure);
        if (baseLevel is null)
            throw new ApiException(ErrorCodes.ParLevelBaseRequired, $"Configure a base par level for '{article.Name}' at this warehouse before adding an override.", 400);

        var typeCode = ParLevelOverrideTypeCodes.ToCode(type);

        if (type == ParLevelOverrideType.Seasonal)
        {
            if (startMonth is null || startDay is null || endMonth is null || endDay is null)
                throw new ApiException(ErrorCodes.ParLevelOverrideInvalidDateRange, "Start/end month and day are required for a seasonal override.", 400);

            if (!ParLevelDateValidation.IsValidMonthDay(startMonth.Value, startDay.Value) || !ParLevelDateValidation.IsValidMonthDay(endMonth.Value, endDay.Value))
                throw new ApiException(ErrorCodes.ParLevelOverrideInvalidDateRange, "Invalid start/end date — note Feb 29 is not supported as a seasonal boundary.", 400);
        }
        else
        {
            if (startDate is null || endDate is null)
                throw new ApiException(ErrorCodes.ParLevelOverrideInvalidDateRange, "Start/end date are required for an event override.", 400);

            if (startDate.Value > endDate.Value)
                throw new ApiException(ErrorCodes.ParLevelOverrideInvalidDateRange, "The start date must not be after the end date.", 400);
        }

        var sameTypeOverrides = (await connection.QueryAsync<ParLevelOverride>(
            "sp_ParLevelOverride_GetByWarehouseAndArticle",
            new { warehouse.WarehouseId, article.ArticleId, Type = typeCode },
            commandType: CommandType.StoredProcedure)).ToList();

        var kind = type == ParLevelOverrideType.Seasonal ? "seasonal" : "event";

        if (TryFindOverlap(type, sameTypeOverrides, startMonth, startDay, endMonth, endDay, startDate, endDate, out var conflictLabel))
            throw new ApiException(ErrorCodes.ParLevelOverrideOverlap, $"This date range overlaps an existing {kind} override ('{conflictLabel}').", 400);

        // DB-level backstop against a TOCTOU race identical in shape to ArticleDiscount's
        // (2026-08-07 audit finding #4, fixed the same day): two genuinely-concurrent creates for
        // the same (Warehouse, Article, Type) scope could both pass the pre-check above before
        // either INSERT landed. sp_getapplock scoped to that triple serializes concurrent writers;
        // the overlap check re-runs once the lock is held so the loser sees the winner's
        // now-committed row. Kept as a C#-side re-check (not pushed into the SP, unlike
        // ArticleDiscount's own fix) because the seasonal-wraparound decomposition logic
        // (DecomposeSeasonalRanges) only exists in C# — duplicating it in T-SQL would be a
        // correctness/drift risk this codebase avoids; TryFindOverlap is the single source of
        // truth both the pre-check and this backstop call.
        var lockResource = $"ParLevelOverrideScope:{warehouse.WarehouseId}:{article.ArticleId}:{typeCode}";

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var lockParams = new DynamicParameters();
            lockParams.Add("@Resource", lockResource);
            lockParams.Add("@LockMode", "Exclusive");
            lockParams.Add("@LockOwner", "Transaction");
            lockParams.Add("@LockTimeout", 10000);
            lockParams.Add("@ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            await connection.ExecuteAsync("sp_getapplock", lockParams, transaction, commandType: CommandType.StoredProcedure);
            var lockResult = lockParams.Get<int>("@ReturnValue");
            if (lockResult < 0)
                throw new ApiException(ErrorCodes.ParLevelOverrideLockTimeout, "Another update to this article's par level overrides is in progress — please retry.", 409);

            var lockedSameTypeOverrides = (await connection.QueryAsync<ParLevelOverride>(
                "sp_ParLevelOverride_GetByWarehouseAndArticle",
                new { warehouse.WarehouseId, article.ArticleId, Type = typeCode },
                transaction, commandType: CommandType.StoredProcedure)).ToList();

            if (TryFindOverlap(type, lockedSameTypeOverrides, startMonth, startDay, endMonth, endDay, startDate, endDate, out var lockedConflictLabel))
                throw new ApiException(ErrorCodes.ParLevelOverrideOverlap, $"This date range overlaps an existing {kind} override ('{lockedConflictLabel}').", 400);

            var created = await connection.QueryFirstOrDefaultAsync<ParLevelOverride>(
                "sp_ParLevelOverride_Create",
                new
                {
                    ParLevelOverrideToken = Guid.NewGuid(),
                    warehouse.WarehouseId,
                    article.ArticleId,
                    Type = typeCode,
                    Label = label,
                    MinimumQuantity = minimumQuantity,
                    ReorderQuantity = reorderQuantity,
                    StartMonth = startMonth,
                    StartDay = startDay,
                    EndMonth = endMonth,
                    EndDay = endDay,
                    StartDate = startDate?.ToDateTime(TimeOnly.MinValue),
                    EndDate = endDate?.ToDateTime(TimeOnly.MinValue),
                    CreatedBy = context.ActorUserToken.ToString()
                },
                transaction, commandType: CommandType.StoredProcedure);

            await transaction.CommitAsync(cancellationToken);

            return created is null ? null : mapper.Map<ParLevelOverrideDto>(created);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> DeleteOverrideAsync(Guid parLevelOverrideToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<ParLevelOverride>(
            "sp_ParLevelOverride_GetByToken", new { ParLevelOverrideToken = parLevelOverrideToken }, commandType: CommandType.StoredProcedure);
        if (existing is null)
            throw new ApiException(ErrorCodes.ParLevelOverrideNotFound, "Par level override not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId, existing.WarehouseId))
            throw new ApiException(ErrorCodes.ParLevelForbidden, "Cannot delete a par level override outside your scope.", 403);

        var deleted = await connection.QueryFirstOrDefaultAsync<bool>(
            "sp_ParLevelOverride_Delete", new { ParLevelOverrideToken = parLevelOverrideToken }, commandType: CommandType.StoredProcedure);

        return deleted;
    }

    public async Task<ParLevelConfigurationDto?> GetConfigurationAsync(Guid warehouseToken, Guid articleToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken }, commandType: CommandType.StoredProcedure);
        if (warehouse is null || !await CanReadOrganizationAsync(connection, context, warehouse.OrganizationId, warehouse.WarehouseId))
            return null;

        var article = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = articleToken }, commandType: CommandType.StoredProcedure);
        if (article is null)
            return null;

        var baseLevel = await connection.QueryFirstOrDefaultAsync<ParLevel>(
            "sp_ParLevel_GetByWarehouseAndArticle", new { warehouse.WarehouseId, article.ArticleId }, commandType: CommandType.StoredProcedure);

        var overrides = await connection.QueryAsync<ParLevelOverride>(
            "sp_ParLevelOverride_GetByWarehouseAndArticle",
            new { warehouse.WarehouseId, article.ArticleId, Type = (string?)null },
            commandType: CommandType.StoredProcedure);

        var effective = await connection.QueryFirstOrDefaultAsync<ParLevelEffective>(
            "sp_ParLevel_GetEffective",
            new { warehouse.WarehouseId, article.ArticleId, AsOfDate = DateTime.UtcNow.Date },
            commandType: CommandType.StoredProcedure);

        return new ParLevelConfigurationDto
        {
            Base = baseLevel is null ? null : mapper.Map<ParLevelDto>(baseLevel),
            Overrides = mapper.MapList<ParLevelOverrideDto>(overrides),
            EffectiveToday = effective is null ? null : mapper.Map<ParLevelEffectiveDto>(effective)
        };
    }

    public async Task<PagedResult<BelowParRowDto>> GetBelowParAsync(Guid? warehouseToken, string? searchText, int? familyId, int? subFamilyId, int? categoryId, int? subCategoryId, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        // Defaults to the caller's own WarehouseId (WarehouseContact login) so an unfiltered
        // request never falls through to "every warehouse in the org" — an explicit
        // warehouseToken is still validated against it below.
        int? warehouseId = context.WarehouseId;
        int? rootOrganizationId = null;

        if (warehouseToken.HasValue)
        {
            var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
                "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken.Value }, commandType: CommandType.StoredProcedure);

            if (warehouse is null || !await CanReadOrganizationAsync(connection, context, warehouse.OrganizationId, warehouse.WarehouseId))
                return new PagedResult<BelowParRowDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            warehouseId = warehouse.WarehouseId;
        }
        else if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            rootOrganizationId = null; // unrestricted
        }
        else if (context.OrganizationId.HasValue)
        {
            rootOrganizationId = context.OrganizationId.Value;
        }
        else
        {
            return new PagedResult<BelowParRowDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@WarehouseId", warehouseId);
        p.Add("@ArticleId", (int?)null);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim());
        p.Add("@FamilyId", familyId);
        p.Add("@SubFamilyId", subFamilyId);
        p.Add("@CategoryId", categoryId);
        p.Add("@SubCategoryId", subCategoryId);
        p.Add("@AsOfDate", DateTime.UtcNow.Date);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<ParLevelBelowParRow>(
            "sp_ParLevel_GetBelowPar", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<BelowParRowDto>
        {
            Items = mapper.MapList<BelowParRowDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }
}
