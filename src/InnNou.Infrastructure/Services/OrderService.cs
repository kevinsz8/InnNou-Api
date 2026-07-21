using ClosedXML.Excel;
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

public class OrderService(IDbConnectionFactory connectionFactory, IMapper mapper) : IOrderService
{
    private sealed class OrderPageRow : Order { public int TotalCount { get; set; } }

    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;
    private const int MaxBulkImportRows = 500;

    // RoleLevel >= 100 manages any organization. RoleLevel >= 20 (Staff) manages only within
    // their own organization's hierarchy (root-or-descendant). Below that, or with no
    // OrganizationId (e.g. a Supplier-scoped login), no access at all — never unrestricted.
    // This is read/write-neutral hierarchy access — used directly by GetByTokenAsync (reads),
    // and as the base for CanManageOrganizationAsync (writes) below.
    private static async Task<bool> CanAccessOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return true;

        if (context.RoleLevel < StaffRoleLevel || !context.OrganizationId.HasValue)
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    // Only a caller whose own organization is ASSOCIATE (the property level) may write — both
    // SuperAdmin (no organization of their own, unless impersonating) and SUPER_ASSOCIATE (the
    // holding/group level) are read-only for Orders, no exceptions; purchasing happens at the
    // ASSOCIATE level only. The holding org and SuperAdmin can still see everything
    // (GetPagedAsync/GetByTokenAsync are unaffected — they call CanAccessOrganizationAsync
    // directly, which keeps its own RoleLevel>=100 bypass for reads) but never
    // creates/edits/submits/cancels/deletes an order itself. Impersonating an ASSOCIATE
    // organization's user (or one of its warehouse contacts) flips the effective
    // OrganizationTypeCode to ASSOCIATE for that session, which is what grants write access —
    // not the RoleLevel. Used to gate every write call site (Create/AddLine/EditLine/
    // DeleteLine/Submit/Cancel/Delete).
    private static async Task<bool> CanManageOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
    {
        if (context.OrganizationTypeCode != OrganizationTypeCodes.Associate)
            return false;

        if (context.RoleLevel < StaffRoleLevel || !context.OrganizationId.HasValue)
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    private static async Task<List<OrderLine>> GetLinesAsync(IDbConnection connection, int orderId)
    {
        var lines = await connection.QueryAsync<OrderLine>(
            "sp_OrderLine_GetByOrderId", new { OrderId = orderId }, commandType: CommandType.StoredProcedure);
        return lines.ToList();
    }

    public async Task<PagedResult<OrderDto>> GetPagedAsync(Guid? warehouseToken, string? status, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        int? rootOrganizationId;
        if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            rootOrganizationId = null;
        }
        else if (context.RoleLevel >= StaffRoleLevel && context.OrganizationId.HasValue)
        {
            rootOrganizationId = context.OrganizationId.Value;
        }
        else
        {
            return new PagedResult<OrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
        }

        int? warehouseId = null;
        if (warehouseToken.HasValue)
        {
            var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
                "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken.Value }, commandType: CommandType.StoredProcedure);
            if (warehouse is null)
                return new PagedResult<OrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
            warehouseId = warehouse.WarehouseId;
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@WarehouseId", warehouseId);
        p.Add("@Status", status);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<OrderPageRow>(
            "sp_Order_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<OrderDto>
        {
            Items = mapper.MapList<OrderDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<OrderDto?> GetByTokenAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = orderToken }, commandType: CommandType.StoredProcedure);

        if (order is null || !await CanAccessOrganizationAsync(connection, context, order.OrganizationId))
            return null;

        var dto = mapper.Map<OrderDto>(order);
        dto.Lines = mapper.MapList<OrderLineDto>(await GetLinesAsync(connection, order.OrderId));
        dto.LineCount = dto.Lines.Count;
        return dto;
    }

    public async Task<OrderDto?> CreateAsync(Guid warehouseToken, string? notes, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken }, commandType: CommandType.StoredProcedure);

        if (warehouse is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, warehouse.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot create an order for a warehouse outside your organization.", 403);

        var p = new DynamicParameters();
        p.Add("@OrderToken", Guid.NewGuid());
        p.Add("@OrganizationId", warehouse.OrganizationId);
        p.Add("@WarehouseId", warehouse.WarehouseId);
        p.Add("@Notes", notes);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());

        var created = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_Create", p, commandType: CommandType.StoredProcedure);

        return created is null ? null : mapper.Map<OrderDto>(created);
    }

    public async Task<OrderLineDto?> AddLineAsync(Guid orderToken, Guid articleToken, decimal quantity, decimal? manualUnitPrice, string? manualCurrencyCode, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = orderToken }, commandType: CommandType.StoredProcedure);

        if (order is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, order.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot modify an order outside your organization.", 403);

        if (order.Status != "DRAFT")
            throw new ApiException(ErrorCodes.OrderNotDraft, "Only a draft order can be modified.", 409);

        // Pass the Order's OWN organization (never null, never the acting user's role/supplier
        // identity) so supplier visibility is checked strictly against the order's org — a
        // private-supplier article must resolve here for its legitimate owning organization
        // regardless of who's acting, and must never be reachable via this path for an org that
        // can't see it (ContextRoleLevel/ContextSupplierId deliberately left unset — i.e. 0/NULL
        // — so no acting-user identity can bypass this check; see CLAUDE.md, "Supplier
        // global/private scoping").
        var article = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = articleToken, OrganizationId = order.OrganizationId, ContextRoleLevel = 0 }, commandType: CommandType.StoredProcedure);

        if (article is null)
            throw new ApiException(ErrorCodes.ArticleNotFound, "Article not found.", 404);

        // OrderLine no longer duplicates the Article's full packaging chain (that's now N rows
        // in ArticlePackagingLevels, immutable via Supersede same as PurchaseUnitId) — it keeps
        // only a display-friendly total: the Unidad Definida's unit, plus the TOTAL factor from
        // the purchase unit down to it (product of every level's QuantityInParentUnit). This
        // degrades gracefully for any chain depth (1, 2, 3+ levels) without OrderLine needing to
        // know how many levels exist.
        var packagingLevels = (await connection.QueryAsync<ArticlePackagingLevel>(
            "sp_ArticlePackagingLevel_GetByArticleId", new { ArticleId = article.ArticleId }, commandType: CommandType.StoredProcedure)).ToList();
        var definedLevel = packagingLevels.FirstOrDefault(l => l.IsDefinedUnit);
        var totalContentQuantity = packagingLevels.Aggregate(1m, (total, level) => total * level.QuantityInParentUnit);

        // Zone delivery-coverage gate — keyed off the ORDER'S OWN organization, never the
        // acting/impersonating user's (same rule as the article-visibility fetch above). Null
        // OrganizationZoneId ("not yet assigned") or a non-ASSOCIATE org means "not enforced
        // yet" — never a block. Day-of-week is deliberately not considered — coverage on any
        // day is enough to be available for ordering.
        var coverage = await connection.QueryFirstOrDefaultAsync<SupplierDeliveryZoneCoverage>(
            "sp_SupplierDeliveryZone_CheckCoverage",
            new { SupplierId = article.SupplierId, OrganizationId = order.OrganizationId },
            commandType: CommandType.StoredProcedure);

        if (coverage is not null && coverage.EnforcementActive && !coverage.HasCoverage)
            throw new ApiException(ErrorCodes.ArticleSupplierZoneNotCovered,
                "This supplier does not deliver to the order's zone.", 409);

        // Resolve the current price for the Order's organization — same resolution the
        // ArticlePrices "current price" read path already uses (contract-over-global, with
        // the currency-hierarchy fallback baked into the SP itself). Exact param shape mirrors
        // ArticlePriceService.GetCurrentAsync's own call to this SP.
        var priceParams = new DynamicParameters();
        priceParams.Add("@ArticleId", article.ArticleId);
        priceParams.Add("@OrganizationId", order.OrganizationId);
        priceParams.Add("@CurrencyCode", null, DbType.AnsiString, size: 10, direction: ParameterDirection.InputOutput);
        priceParams.Add("@AsOfDate", DateTime.UtcNow.Date);

        var priceRow = await connection.QueryFirstOrDefaultAsync<ArticlePrice>(
            "sp_ArticlePrice_GetCurrent", priceParams, commandType: CommandType.StoredProcedure);

        var resolvedCurrencyCode = priceParams.Get<string?>("@CurrencyCode");

        decimal unitPrice;
        string currencyCode;

        if (priceRow is not null)
        {
            unitPrice = priceRow.Price;
            currencyCode = priceRow.CurrencyCode;
        }
        else
        {
            // SERVICE/MIXED suppliers are not required to carry a catalog ArticlePrice —
            // their price is negotiated at order time instead (see CLAUDE.md's "Supplier
            // type" section). PRODUCT suppliers keep the hard failure: a missing price
            // there is a real catalog data gap, not something to silently paper over.
            var isServiceOrMixed = article.SupplierType is SupplierTypeCodes.Service or SupplierTypeCodes.Mixed;
            if (!isServiceOrMixed)
            {
                if (resolvedCurrencyCode is null)
                    throw new ApiException(ErrorCodes.ArticlePriceCurrencyRequired, "A currency code could not be determined for this organization.", 400);

                throw new ApiException(ErrorCodes.ArticlePriceNotFound, "No current price found for this article.", 404);
            }

            if (!manualUnitPrice.HasValue || manualUnitPrice.Value <= 0 || string.IsNullOrWhiteSpace(manualCurrencyCode))
                throw new ApiException(ErrorCodes.ArticlePriceManualRequired, "This article has no catalog price — provide a manual unit price and currency for this order line.", 400);

            var normalizedCurrencyCode = manualCurrencyCode.Trim().ToUpperInvariant();
            var currencyExists = await connection.ExecuteScalarAsync<bool>(
                "sp_Currency_ExistsByCode", new { Code = normalizedCurrencyCode }, commandType: CommandType.StoredProcedure);
            if (!currencyExists)
                throw new ApiException(ErrorCodes.ArticlePriceInvalidCurrency, "Invalid or inactive currency code.", 400);

            unitPrice = manualUnitPrice.Value;
            currencyCode = normalizedCurrencyCode;
        }

        // Snapshot the Order's own organization's effective classification (own row, else
        // inherited from the nearest Super Asociado ancestor) — frozen onto the OrderLine as
        // plain CategoryId/CategoryCode/SubCategoryId/SubCategoryCode, never re-resolved live.
        // This is what protects historical spend-by-category reporting: a later Article
        // reclassification or Category Code rename must never retroactively change what an
        // already-placed Order reports. An unclassified article simply snapshots nulls —
        // classification is optional metadata, never a purchasing precondition.
        var classification = await connection.QueryFirstOrDefaultAsync<ArticleClassificationEffective>(
            "sp_ArticleClassification_GetEffectiveForArticle",
            new { ArticleId = article.ArticleId, OrganizationId = order.OrganizationId },
            commandType: CommandType.StoredProcedure);

        var linePararms = new DynamicParameters();
        linePararms.Add("@OrderLineToken", Guid.NewGuid());
        linePararms.Add("@OrderId", order.OrderId);
        linePararms.Add("@ArticleId", article.ArticleId);
        linePararms.Add("@Quantity", quantity);
        linePararms.Add("@PurchaseUnitId", article.PurchaseUnitId);
        linePararms.Add("@PurchaseQuantity", 1m);
        linePararms.Add("@ContentUnitId", definedLevel?.UnitOfMeasureId ?? article.PurchaseUnitId);
        linePararms.Add("@ContentQuantity", totalContentQuantity);
        linePararms.Add("@UnitPrice", unitPrice);
        linePararms.Add("@CurrencyCode", currencyCode);
        linePararms.Add("@CategoryId", classification?.CategoryId);
        linePararms.Add("@CategoryCode", classification?.CategoryCode);
        linePararms.Add("@SubCategoryId", classification?.SubCategoryId);
        linePararms.Add("@SubCategoryCode", classification?.SubCategoryCode);
        linePararms.Add("@Notes", (string?)null);
        linePararms.Add("@CreatedBy", context.ActorUserToken.ToString());

        var line = await connection.QueryFirstOrDefaultAsync<OrderLine>(
            "sp_OrderLine_Upsert", linePararms, commandType: CommandType.StoredProcedure);

        return line is null ? null : mapper.Map<OrderLineDto>(line);
    }

    public async Task<OrderLineDto?> EditLineAsync(Guid orderLineToken, decimal quantity, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existingLine = await GetLineByTokenAsync(connection, orderLineToken);
        if (existingLine is null)
            return null;

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = existingLine.OrderToken }, commandType: CommandType.StoredProcedure);

        if (order is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, order.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot modify an order outside your organization.", 403);

        if (order.Status != "DRAFT")
            throw new ApiException(ErrorCodes.OrderNotDraft, "Only a draft order can be modified.", 409);

        var updated = await connection.QueryFirstOrDefaultAsync<OrderLine>(
            "sp_OrderLine_Edit",
            new { OrderLineToken = orderLineToken, Quantity = quantity, LastUpdatedBy = context.ActorUserToken.ToString() },
            commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<OrderLineDto>(updated);
    }

    public async Task<bool> DeleteLineAsync(Guid orderLineToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existingLine = await GetLineByTokenAsync(connection, orderLineToken);
        if (existingLine is null)
            return false;

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = existingLine.OrderToken }, commandType: CommandType.StoredProcedure);

        if (order is null)
            return false;

        if (!await CanManageOrganizationAsync(connection, context, order.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot modify an order outside your organization.", 403);

        if (order.Status != "DRAFT")
            throw new ApiException(ErrorCodes.OrderNotDraft, "Only a draft order can be modified.", 409);

        await connection.ExecuteAsync(
            "sp_OrderLine_Delete", new { OrderLineToken = orderLineToken }, commandType: CommandType.StoredProcedure);

        return true;
    }

    private static Task<OrderLine?> GetLineByTokenAsync(IDbConnection connection, Guid orderLineToken)
    {
        return connection.QueryFirstOrDefaultAsync<OrderLine>(
            "sp_OrderLine_GetByToken", new { OrderLineToken = orderLineToken }, commandType: CommandType.StoredProcedure)!;
    }

    public async Task<OrderDto?> SubmitAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = orderToken }, commandType: CommandType.StoredProcedure);

        if (order is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, order.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot submit an order outside your organization.", 403);

        if (order.Status != "DRAFT")
            throw new ApiException(ErrorCodes.OrderNotDraft, "Only a draft order can be submitted.", 409);

        var lines = await GetLinesAsync(connection, order.OrderId);
        if (lines.Count == 0)
            throw new ApiException(ErrorCodes.OrderEmpty, "Cannot submit an order with no lines.", 400);

        var actor = context.ActorUserToken.ToString();

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var supplierGroup in lines.GroupBy(l => l.SupplierId))
            {
                var poParams = new DynamicParameters();
                poParams.Add("@PurchaseOrderToken", Guid.NewGuid());
                poParams.Add("@OrderId", order.OrderId);
                poParams.Add("@SupplierId", supplierGroup.Key);
                poParams.Add("@OrganizationId", order.OrganizationId);
                poParams.Add("@WarehouseId", order.WarehouseId);
                poParams.Add("@CreatedBy", actor);

                var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
                    "sp_PurchaseOrder_Create", poParams, transaction, commandType: CommandType.StoredProcedure);

                if (purchaseOrder is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }

                // One PurchaseOrderLine per OrderLine in this supplier's group — an
                // independent snapshot copy captured at split time, not a shared row
                // with OrderLine (see .claude/OrdersModule.md for why).
                foreach (var line in supplierGroup)
                {
                    var polParams = new DynamicParameters();
                    polParams.Add("@PurchaseOrderLineToken", Guid.NewGuid());
                    polParams.Add("@PurchaseOrderId", purchaseOrder.PurchaseOrderId);
                    polParams.Add("@OrderLineId", line.OrderLineId);
                    polParams.Add("@ArticleId", line.ArticleId);
                    polParams.Add("@Quantity", line.Quantity);
                    polParams.Add("@PurchaseUnitId", line.PurchaseUnitId);
                    polParams.Add("@PurchaseQuantity", line.PurchaseQuantity);
                    polParams.Add("@ContentUnitId", line.ContentUnitId);
                    polParams.Add("@ContentQuantity", line.ContentQuantity);
                    polParams.Add("@UnitPrice", line.UnitPrice);
                    polParams.Add("@CurrencyCode", line.CurrencyCode);
                    polParams.Add("@CategoryId", line.CategoryId);
                    polParams.Add("@CategoryCode", line.CategoryCode);
                    polParams.Add("@SubCategoryId", line.SubCategoryId);
                    polParams.Add("@SubCategoryCode", line.SubCategoryCode);
                    polParams.Add("@Notes", line.Notes);
                    polParams.Add("@CreatedBy", actor);

                    await connection.ExecuteAsync(
                        "sp_PurchaseOrderLine_Create", polParams, transaction, commandType: CommandType.StoredProcedure);
                }
            }

            var updatedOrder = await connection.QueryFirstOrDefaultAsync<Order>(
                "sp_Order_SetStatus",
                new { OrderToken = orderToken, Status = "SUBMITTED", ActorBy = actor },
                transaction,
                commandType: CommandType.StoredProcedure);

            await transaction.CommitAsync(cancellationToken);

            if (updatedOrder is null)
                return null;

            var dto = mapper.Map<OrderDto>(updatedOrder);
            dto.Lines = mapper.MapList<OrderLineDto>(await GetLinesAsync(connection, order.OrderId));
            dto.LineCount = dto.Lines.Count;
            return dto;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = orderToken }, commandType: CommandType.StoredProcedure);

        if (order is null)
            return false;

        if (!await CanManageOrganizationAsync(connection, context, order.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot delete an order outside your organization.", 403);

        if (order.Status != "DRAFT")
            throw new ApiException(ErrorCodes.OrderNotDraft, "Only a draft order can be deleted.", 409);

        await connection.ExecuteAsync(
            "sp_Order_Delete", new { OrderToken = orderToken }, commandType: CommandType.StoredProcedure);

        return true;
    }

    public async Task<OrderDto?> CancelAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = orderToken }, commandType: CommandType.StoredProcedure);

        if (order is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, order.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot cancel an order outside your organization.", 403);

        if (order.Status != "DRAFT")
            throw new ApiException(ErrorCodes.OrderNotDraft, "Only a draft order can be cancelled.", 409);

        var updated = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_SetStatus",
            new { OrderToken = orderToken, Status = "CANCELLED", ActorBy = context.ActorUserToken.ToString() },
            commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<OrderDto>(updated);
    }

    // Bulk-adds lines to an existing Draft order from an uploaded Excel file. Column layout
    // matches OrderTemplateService.ExportAsync's export exactly: SupplierName, SupplierSku,
    // ArticleName (informational only, never used to resolve), Quantity, Price, CurrencyCode,
    // ArticleToken (hidden, primary match). Row resolution mirrors
    // ArticleService.BulkImportArticlesAsync's precedence: ArticleToken first, falling back to
    // (SupplierName -> SupplierId, SupplierSku). Price/CurrencyCode are only actually honored by
    // AddLineAsync when the resolved article's SupplierType is SERVICE/MIXED and no catalog
    // price resolves — a PRODUCT article's price always comes from the live catalog regardless
    // of what's in the cell, so a client can never override real catalog truth. Unlike
    // ApplyOrderTemplateAsync's per-line handling, ArticlePriceManualRequired here IS a hard row
    // failure — there is no interactive modal possible mid-file-parse, so the Price/CurrencyCode
    // columns exist precisely so the user fills them in before uploading.
    public async Task<ImportOrderLinesResultDto> ImportLinesAsync(Guid orderToken, byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var order = await connection.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetByToken", new { OrderToken = orderToken }, commandType: CommandType.StoredProcedure);

        if (order is null)
            throw new ApiException(ErrorCodes.OrderNotFound, "Order not found.", 404);

        if (!await CanManageOrganizationAsync(connection, context, order.OrganizationId))
            throw new ApiException(ErrorCodes.OrderForbidden, "Cannot modify an order outside your organization.", 403);

        if (order.Status != "DRAFT")
            throw new ApiException(ErrorCodes.OrderNotDraft, "Only a draft order can be modified.", 409);

        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(new MemoryStream(fileBytes));
        }
        catch
        {
            throw new ApiException(ErrorCodes.OrderImportLinesInvalidFile, "The uploaded file is not a valid Excel (.xlsx) file.", 400);
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.First();

            var dataRows = worksheet.RowsUsed()
                .Skip(1)
                .Where(row => row.CellsUsed().Any(c => !string.IsNullOrWhiteSpace(c.GetString())))
                .ToList();

            if (dataRows.Count > MaxBulkImportRows)
                throw new ApiException(ErrorCodes.OrderImportLinesTooManyRows, $"A single import file cannot contain more than {MaxBulkImportRows} rows.", 400);

            var result = new ImportOrderLinesResultDto { TotalRows = dataRows.Count };

            if (dataRows.Count == 0)
                return result;

            var supplierCache = new Dictionary<string, Supplier?>(StringComparer.OrdinalIgnoreCase);

            // Rows are processed strictly sequentially — same convention as every other bulk
            // import in this codebase — one row's failure never aborts the rest.
            foreach (var row in dataRows)
            {
                var rowNumber = row.RowNumber();

                var supplierName = row.Cell(1).GetString().Trim();
                var supplierSku = row.Cell(2).GetString().Trim();
                var quantityText = row.Cell(4).GetString().Trim();
                var priceText = row.Cell(5).GetString().Trim();
                var currencyCodeText = row.Cell(6).GetString().Trim();
                var articleTokenText = row.Cell(7).GetString().Trim();

                var rowIdentifier = !string.IsNullOrWhiteSpace(supplierSku) ? supplierSku : (string.IsNullOrWhiteSpace(articleTokenText) ? null : articleTokenText);

                if (!decimal.TryParse(quantityText, out var quantity) || quantity <= 0)
                {
                    result.Errors.Add(new ImportOrderLinesRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.OrderImportLinesRowInvalid, Description = "Quantity is required and must be greater than zero." });
                    continue;
                }

                Article? article;
                if (!string.IsNullOrWhiteSpace(articleTokenText))
                {
                    if (!Guid.TryParse(articleTokenText, out var articleToken))
                    {
                        result.Errors.Add(new ImportOrderLinesRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.OrderImportLinesRowInvalid, Description = "ArticleToken is not a valid identifier." });
                        continue;
                    }

                    // Pass the Order's OWN organization, never the acting user's identity — see
                    // AddLineAsync's comment above for the full reasoning.
                    article = await connection.QueryFirstOrDefaultAsync<Article>(
                        "sp_Article_GetByToken", new { ArticleToken = articleToken, OrganizationId = order.OrganizationId, ContextRoleLevel = 0 }, commandType: CommandType.StoredProcedure);

                    if (article is null)
                    {
                        result.Errors.Add(new ImportOrderLinesRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleNotFound, Description = $"No article found for ArticleToken '{articleTokenText}'." });
                        continue;
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(supplierName) || string.IsNullOrWhiteSpace(supplierSku))
                    {
                        result.Errors.Add(new ImportOrderLinesRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.OrderImportLinesRowInvalid, Description = "SupplierName and SupplierSku are required when ArticleToken is blank." });
                        continue;
                    }

                    var supplierKey = supplierName.ToUpperInvariant();
                    if (!supplierCache.TryGetValue(supplierKey, out var supplier))
                    {
                        supplier = await connection.QueryFirstOrDefaultAsync<Supplier>(
                            "sp_Supplier_GetByNormalizedName", new { NormalizedName = supplierKey }, commandType: CommandType.StoredProcedure);
                        supplierCache[supplierKey] = supplier;
                    }

                    if (supplier is null)
                    {
                        result.Errors.Add(new ImportOrderLinesRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.SupplierNotFound, Description = $"Supplier '{supplierName}' was not found." });
                        continue;
                    }

                    article = await connection.QueryFirstOrDefaultAsync<Article>(
                        "sp_Article_GetBySupplierSku", new { SupplierId = supplier.SupplierId, SupplierSku = supplierSku }, commandType: CommandType.StoredProcedure);

                    if (article is null)
                    {
                        result.Errors.Add(new ImportOrderLinesRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleNotFound, Description = $"No article found for SupplierSku '{supplierSku}'." });
                        continue;
                    }
                }

                decimal? manualUnitPrice = null;
                if (!string.IsNullOrWhiteSpace(priceText) && decimal.TryParse(priceText, out var parsedPrice) && parsedPrice > 0)
                    manualUnitPrice = parsedPrice;
                var manualCurrencyCode = string.IsNullOrWhiteSpace(currencyCodeText) ? null : currencyCodeText.Trim();

                try
                {
                    await AddLineAsync(orderToken, article.ArticleToken, quantity, manualUnitPrice, manualCurrencyCode, context, cancellationToken);
                    result.SucceededCount++;
                }
                catch (ApiException ex)
                {
                    result.Errors.Add(new ImportOrderLinesRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ex.Code, Description = ex.Message });
                }
                catch (Exception)
                {
                    result.Errors.Add(new ImportOrderLinesRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.UnhandledError, Description = "An unexpected error occurred while processing this row." });
                }
            }

            result.FailureCount = result.Errors.Count;
            return result;
        }
    }

    private sealed class SupplierDeliveryZoneCoverage
    {
        public int? OrganizationZoneId { get; set; }
        public bool EnforcementActive { get; set; }
        public bool HasCoverage { get; set; }
    }

    private sealed class ArticleClassificationEffective
    {
        public int? CategoryId { get; set; }
        public string? CategoryCode { get; set; }
        public int? SubCategoryId { get; set; }
        public string? SubCategoryCode { get; set; }
        public bool IsInherited { get; set; }
    }
}
