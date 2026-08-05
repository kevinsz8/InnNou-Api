using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace InnNou.Infrastructure.Services;

public class InternalOrderService(IDbConnectionFactory connectionFactory, IMapper mapper, INotificationService notificationService, ILogger<InternalOrderService> logger) : IInternalOrderService
{
    private sealed class InternalOrderPageRow : InternalOrder { public int TotalCount { get; set; } }

    private sealed class ValidatedInternalOrderLine
    {
        public Article Article { get; set; } = default!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string CurrencyCode { get; set; } = default!;
        public string? Notes { get; set; }
    }

    private sealed class ValidatedShipmentLine
    {
        public InternalOrderLine Line { get; set; } = default!;
        public decimal QuantityShipped { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class ValidatedReceiptLine
    {
        public InternalOrderShipmentLine ShipmentLine { get; set; } = default!;
        public decimal UnitPrice { get; set; }
        public decimal QuantityAccepted { get; set; }
        public decimal QuantityRejected { get; set; }
        public string? RejectionReason { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class InternalOrderLineTax
    {
        public int TaxCategoryId { get; set; }
        public int TaxRateId { get; set; }
        public decimal TaxRatePercent { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    // Read visibility, no OrganizationTypeCode restriction — mirrors InventoryService's own
    // CanReadOrganizationAsync (PurchaseOrderService/OrderService's original shape).
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

    // Write visibility — mirrors InventoryService/OrderService.CanManageOrganizationAsync: only a
    // caller whose own organization is ASSOCIATE may write; SuperAdmin (no organization of their
    // own, unless impersonating) and SUPER_ASSOCIATE are read-only — an Internal Order is always
    // acted on at the property level, one specific Asociado at a time (as either the requesting
    // or the source side), same reasoning as Orders/Inventory.
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

    // Best-effort/non-blocking. Recipient is resolved from the InternalOrder's own CreatedBy —
    // the requesting organization's user who originally created the request — never
    // context.ActorUserToken, since shipping/receiving/cancelling can legitimately be done by a
    // different person on the same team (same "resolve from the entity, not the acting context"
    // principle as OrderService.NotifyOrderSubmitterAsync).
    private async Task NotifyRequestingOrganizationAsync(DbConnection connection, InternalOrder header, NotificationType type, object data, IRequestContext context, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(header.CreatedBy, out var requesterToken))
            return;

        try
        {
            // notificationService.NotifyAsync opens its own connection — close the caller's first
            // so at most one is ever open at once (Dapper transparently reopens it on next use).
            // See PurchaseOrderService.NotifyOrderBuyerAsync's own comment for the full reasoning
            // (integration tests wrap everything in an ambient TransactionScope that can't survive
            // two simultaneously-open connections without MSDTC).
            await connection.CloseAsync();

            await notificationService.NotifyAsync(requesterToken, type, data, $"/internalOrders/{header.InternalOrderToken}", context, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Notification failed for InternalOrder {InternalOrderToken}", header.InternalOrderToken);
        }
    }

    public async Task<InternalOrderDto?> CreateAsync(Guid sourceOrganizationToken, Guid destinationWarehouseToken, string? notes, List<CreateInternalOrderLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var destinationWarehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = destinationWarehouseToken }, commandType: CommandType.StoredProcedure);
        if (destinationWarehouse is null)
            throw new ApiException(ErrorCodes.InternalOrderDestinationWarehouseNotFound, "Destination warehouse not found.", 404);

        // The requesting Organization is inferred from the destination Warehouse's own owner —
        // never a caller-supplied value — same reasoning Order/PurchaseOrder use for their own
        // WarehouseId->OrganizationId derivation.
        if (!await CanManageOrganizationAsync(connection, context, destinationWarehouse.OrganizationId, destinationWarehouse.WarehouseId))
            throw new ApiException(ErrorCodes.InternalOrderForbidden, "Cannot request an internal order for a warehouse outside your scope.", 403);

        if (!destinationWarehouse.IsInventoriable)
            throw new ApiException(ErrorCodes.InternalOrderDestinationWarehouseNotInventoriable, "The destination warehouse does not track inventory.", 400);

        var sourceOrganization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken", new { OrganizationToken = sourceOrganizationToken }, commandType: CommandType.StoredProcedure);
        if (sourceOrganization is null)
            throw new ApiException(ErrorCodes.InternalOrderSourceOrganizationNotFound, "Source organization not found.", 404);

        if (sourceOrganization.OrganizationId == destinationWarehouse.OrganizationId)
            throw new ApiException(ErrorCodes.InternalOrderSameOrganization, "The source organization must be different from the requesting organization.", 400);

        // Confirmed scope for V1: any Asociado may internal-order from any other Asociado under
        // the same Super Asociado — no separate configurable relationship table. See
        // CLAUDE.md's "Internal Orders" section.
        var requestingSuperAssociateId = await connection.QueryFirstOrDefaultAsync<int?>(
            "sp_Organization_GetNearestSuperAssociateAncestor", new { OrganizationId = destinationWarehouse.OrganizationId }, commandType: CommandType.StoredProcedure);
        var sourceSuperAssociateId = await connection.QueryFirstOrDefaultAsync<int?>(
            "sp_Organization_GetNearestSuperAssociateAncestor", new { OrganizationId = sourceOrganization.OrganizationId }, commandType: CommandType.StoredProcedure);

        if (!requestingSuperAssociateId.HasValue || !sourceSuperAssociateId.HasValue || requestingSuperAssociateId != sourceSuperAssociateId)
            throw new ApiException(ErrorCodes.InternalOrderDifferentSuperAssociate, "Both organizations must belong to the same Super Asociado.", 400);

        if (lines.Count == 0)
            throw new ApiException(ErrorCodes.InternalOrderEmpty, "At least one line must be requested.", 400);

        var validatedLines = new List<ValidatedInternalOrderLine>();
        var requestedArticleIds = new HashSet<int>();

        foreach (var input in lines)
        {
            if (input.Quantity <= 0)
                throw new ApiException(ErrorCodes.InternalOrderInvalidQuantity, "Requested quantity must be greater than zero.", 400);

            // Same visibility check OrderService.AddLineAsync uses (ContextRoleLevel = 0, never
            // the acting user's own role/supplier identity) — a private-supplier article must
            // resolve here for the requesting organization's own legitimate scope only.
            var article = await connection.QueryFirstOrDefaultAsync<Article>(
                "sp_Article_GetByToken", new { ArticleToken = input.ArticleToken, OrganizationId = destinationWarehouse.OrganizationId, ContextRoleLevel = 0 }, commandType: CommandType.StoredProcedure);
            if (article is null)
                throw new ApiException(ErrorCodes.InternalOrderArticleNotFound, $"Article '{input.ArticleToken}' not found.", 404);

            if (!requestedArticleIds.Add(article.ArticleId))
                throw new ApiException(ErrorCodes.InternalOrderDuplicateLine, $"Article '{article.Name}' was submitted more than once.", 400);

            // Price is always the destination Organization's own resolved ArticlePrice — never a
            // caller-supplied value, no manual-price fallback (unlike Order.AddLineAsync's
            // SERVICE/MIXED-supplier exception, which doesn't apply here — there is no Supplier
            // in an Internal Order at all). See CLAUDE.md's "Internal Orders" section.
            var priceParams = new DynamicParameters();
            priceParams.Add("@ArticleId", article.ArticleId);
            priceParams.Add("@OrganizationId", destinationWarehouse.OrganizationId);
            priceParams.Add("@CurrencyCode", null, DbType.AnsiString, size: 10, direction: ParameterDirection.InputOutput);
            priceParams.Add("@AsOfDate", DateTime.UtcNow.Date);

            var priceRow = await connection.QueryFirstOrDefaultAsync<ArticlePrice>(
                "sp_ArticlePrice_GetCurrent", priceParams, commandType: CommandType.StoredProcedure);

            if (priceRow is null)
                throw new ApiException(ErrorCodes.InternalOrderPriceNotFound, $"No current price found for article '{article.Name}' at the destination organization.", 404);

            validatedLines.Add(new ValidatedInternalOrderLine
            {
                Article = article,
                Quantity = input.Quantity,
                UnitPrice = priceRow.Price,
                CurrencyCode = priceRow.CurrencyCode,
                Notes = input.Notes
            });
        }

        var actor = context.ActorUserToken.ToString();

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var headerParams = new DynamicParameters();
            headerParams.Add("@InternalOrderToken", Guid.NewGuid());
            headerParams.Add("@RequestingOrganizationId", destinationWarehouse.OrganizationId);
            headerParams.Add("@SourceOrganizationId", sourceOrganization.OrganizationId);
            headerParams.Add("@DestinationWarehouseId", destinationWarehouse.WarehouseId);
            headerParams.Add("@Notes", notes);
            headerParams.Add("@CreatedBy", actor);

            var header = await connection.QueryFirstOrDefaultAsync<InternalOrder>(
                "sp_InternalOrder_Create", headerParams, transaction, commandType: CommandType.StoredProcedure);

            if (header is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            foreach (var validated in validatedLines)
            {
                var lineParams = new DynamicParameters();
                lineParams.Add("@InternalOrderLineToken", Guid.NewGuid());
                lineParams.Add("@InternalOrderId", header.InternalOrderId);
                lineParams.Add("@ArticleId", validated.Article.ArticleId);
                lineParams.Add("@Quantity", validated.Quantity);
                lineParams.Add("@UnitPrice", validated.UnitPrice);
                lineParams.Add("@CurrencyCode", validated.CurrencyCode);
                lineParams.Add("@Notes", validated.Notes);
                lineParams.Add("@CreatedBy", actor);

                await connection.ExecuteAsync("sp_InternalOrderLine_Create", lineParams, transaction, commandType: CommandType.StoredProcedure);
            }

            await transaction.CommitAsync(cancellationToken);

            var dto = mapper.Map<InternalOrderDto>(header);
            dto.Lines = mapper.MapList<InternalOrderLineDto>(
                await connection.QueryAsync<InternalOrderLine>(
                    "sp_InternalOrderLine_GetByInternalOrderId", new { header.InternalOrderId }, commandType: CommandType.StoredProcedure));
            dto.LineCount = dto.Lines.Count;

            return dto;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<InternalOrderDto?> GetByTokenAsync(Guid internalOrderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var header = await connection.QueryFirstOrDefaultAsync<InternalOrder>(
            "sp_InternalOrder_GetByToken", new { InternalOrderToken = internalOrderToken }, commandType: CommandType.StoredProcedure);
        if (header is null)
            return null;

        var shipments = (await connection.QueryAsync<InternalOrderShipment>(
            "sp_InternalOrderShipment_GetByInternalOrderId", new { header.InternalOrderId }, commandType: CommandType.StoredProcedure)).ToList();

        // Either party to the InternalOrder (requesting or source) may read it — the reverse of
        // ConsolidatedPurchaseOrder's own "only the owner sees it" rule, since here both sides
        // are real counterparties to the same document. On the requesting side a warehouse-scoped
        // caller must be the destination warehouse itself; on the source side (no single source
        // warehouse lives on the header — SourceWarehouseId is chosen per-Shipment) they must own
        // at least one of the shipments actually sent so far, or — before any shipment exists yet
        // — the org-level check alone stands (nothing more specific to narrow against).
        var canReadAsRequester = await CanReadOrganizationAsync(connection, context, header.RequestingOrganizationId, header.DestinationWarehouseId);
        var canReadAsSource = await CanReadOrganizationAsync(connection, context, header.SourceOrganizationId)
            && (shipments.Count == 0 || shipments.Any(s => WarehouseScopeGuard.Allows(context, s.SourceWarehouseId)));

        if (!canReadAsRequester && !canReadAsSource)
            return null;

        var dto = mapper.Map<InternalOrderDto>(header);

        dto.Lines = mapper.MapList<InternalOrderLineDto>(
            await connection.QueryAsync<InternalOrderLine>(
                "sp_InternalOrderLine_GetByInternalOrderId", new { header.InternalOrderId }, commandType: CommandType.StoredProcedure));
        dto.LineCount = dto.Lines.Count;

        foreach (var shipment in shipments)
        {
            var shipmentDto = mapper.Map<InternalOrderShipmentDto>(shipment);
            shipmentDto.Lines = mapper.MapList<InternalOrderShipmentLineDto>(
                await connection.QueryAsync<InternalOrderShipmentLine>(
                    "sp_InternalOrderShipmentLine_GetByInternalOrderShipmentId", new { shipment.InternalOrderShipmentId }, commandType: CommandType.StoredProcedure));
            dto.Shipments.Add(shipmentDto);
        }

        var receipts = (await connection.QueryAsync<InternalOrderReceipt>(
            "sp_InternalOrderReceipt_GetByInternalOrderId", new { header.InternalOrderId }, commandType: CommandType.StoredProcedure)).ToList();

        foreach (var receipt in receipts)
        {
            var receiptDto = mapper.Map<InternalOrderReceiptDto>(receipt);
            receiptDto.Lines = mapper.MapList<InternalOrderReceiptLineDto>(
                await connection.QueryAsync<InternalOrderReceiptLine>(
                    "sp_InternalOrderReceiptLine_GetByInternalOrderReceiptId", new { receipt.InternalOrderReceiptId }, commandType: CommandType.StoredProcedure));
            dto.Receipts.Add(receiptDto);
        }

        return dto;
    }

    public async Task<PagedResult<InternalOrderDto>> GetPagedAsync(string? direction, string? status, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        int? contextOrganizationId = null;
        if (context.RoleLevel < SuperAdminRoleLevel)
        {
            if (!context.OrganizationId.HasValue)
                return new PagedResult<InternalOrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            contextOrganizationId = context.OrganizationId.Value;
        }

        var rows = (await connection.QueryAsync<InternalOrderPageRow>(
            "sp_InternalOrder_GetPaged",
            new
            {
                PageNumber = safePageNumber,
                PageSize = safePageSize,
                ContextOrganizationId = contextOrganizationId,
                DirectionFilter = direction,
                Status = status,
                ContextWarehouseId = context.WarehouseId
            },
            commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<InternalOrderDto>
        {
            Items = mapper.MapList<InternalOrderDto>(rows),
            TotalCount = rows.Count > 0 ? rows[0].TotalCount : 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<InternalOrderDto?> CancelAsync(Guid internalOrderToken, string reason, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var header = await connection.QueryFirstOrDefaultAsync<InternalOrder>(
            "sp_InternalOrder_GetByToken", new { InternalOrderToken = internalOrderToken }, commandType: CommandType.StoredProcedure);
        if (header is null)
            return null;

        // Only the requesting Organization may cancel its own request — the source Organization
        // fulfills, it doesn't get to withdraw someone else's request.
        if (!await CanManageOrganizationAsync(connection, context, header.RequestingOrganizationId, header.DestinationWarehouseId))
            throw new ApiException(ErrorCodes.InternalOrderForbidden, "Cannot cancel an internal order outside your scope.", 403);

        if (header.Status != InternalOrderStatusCodes.Requested)
            throw new ApiException(ErrorCodes.InternalOrderNotCancellable, "Only a requested internal order (not yet shipped) can be cancelled.", 409);

        var updated = await connection.QueryFirstOrDefaultAsync<InternalOrder>(
            "sp_InternalOrder_Cancel",
            new { InternalOrderToken = internalOrderToken, CancelledBy = context.ActorUserToken.ToString(), CancelledReason = reason },
            commandType: CommandType.StoredProcedure);

        if (updated is null)
            return null;

        await NotifyRequestingOrganizationAsync(
            connection, updated, NotificationType.Internal_Order_Cancelled,
            new { internalOrderNumber = updated.InternalOrderNumber, reason },
            context, cancellationToken);

        return mapper.Map<InternalOrderDto>(updated);
    }

    public async Task<InternalOrderShipmentDto?> CreateShipmentAsync(Guid internalOrderToken, Guid sourceWarehouseToken, string? notes, List<CreateInternalOrderShipmentLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var header = await connection.QueryFirstOrDefaultAsync<InternalOrder>(
            "sp_InternalOrder_GetByToken", new { InternalOrderToken = internalOrderToken }, commandType: CommandType.StoredProcedure);
        if (header is null)
            return null;

        // Only the source Organization ships — it's fulfilling the requesting Organization's own
        // request.
        if (!await CanManageOrganizationAsync(connection, context, header.SourceOrganizationId))
            throw new ApiException(ErrorCodes.InternalOrderForbidden, "Cannot ship an internal order outside your scope.", 403);

        if (header.Status is InternalOrderStatusCodes.Received or InternalOrderStatusCodes.Cancelled)
            throw new ApiException(ErrorCodes.InternalOrderNotShippable, "This internal order can no longer be shipped.", 409);

        var sourceWarehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = sourceWarehouseToken }, commandType: CommandType.StoredProcedure);
        if (sourceWarehouse is null)
            throw new ApiException(ErrorCodes.InternalOrderSourceWarehouseNotFound, "Source warehouse not found.", 404);

        if (sourceWarehouse.OrganizationId != header.SourceOrganizationId)
            throw new ApiException(ErrorCodes.InternalOrderShipmentSourceWarehouseMismatch, "The shipping warehouse must belong to the internal order's source organization.", 400);

        if (!WarehouseScopeGuard.Allows(context, sourceWarehouse.WarehouseId))
            throw new ApiException(ErrorCodes.InternalOrderForbidden, "Cannot ship an internal order from a warehouse outside your scope.", 403);

        if (!sourceWarehouse.IsInventoriable)
            throw new ApiException(ErrorCodes.InternalOrderShipmentWarehouseNotInventoriable, "The source warehouse does not track inventory.", 400);

        if (!sourceWarehouse.CanTransferOut)
            throw new ApiException(ErrorCodes.InternalOrderShipmentWarehouseCannotTransferOut, "The source warehouse is not configured to transfer inventory out.", 400);

        if (lines.Count == 0)
            throw new ApiException(ErrorCodes.InternalOrderShipmentEmpty, "At least one line must be shipped.", 400);

        var orderLines = (await connection.QueryAsync<InternalOrderLine>(
            "sp_InternalOrderLine_GetByInternalOrderId", new { header.InternalOrderId }, commandType: CommandType.StoredProcedure))
            .ToDictionary(l => l.InternalOrderLineToken);

        var stockByArticle = (await connection.QueryAsync<StockLevel>(
            "sp_StockLevel_GetAllByWarehouseId", new { sourceWarehouse.WarehouseId }, commandType: CommandType.StoredProcedure))
            .ToDictionary(s => s.ArticleId, s => s.QuantityOnHand);

        var validatedLines = new List<ValidatedShipmentLine>();
        var requestedLineIds = new HashSet<int>();

        foreach (var input in lines)
        {
            if (!orderLines.TryGetValue(input.InternalOrderLineToken, out var line))
                throw new ApiException(ErrorCodes.InternalOrderShipmentLineNotFound, $"Internal order line '{input.InternalOrderLineToken}' does not belong to this internal order.", 404);

            if (!requestedLineIds.Add(line.InternalOrderLineId))
                throw new ApiException(ErrorCodes.InternalOrderDuplicateLine, $"Article '{line.ArticleName}' was submitted more than once.", 400);

            if (input.QuantityShipped <= 0)
                throw new ApiException(ErrorCodes.InternalOrderInvalidQuantity, "Shipped quantity must be greater than zero.", 400);

            var remainingToShip = line.Quantity - line.QuantityShipped;
            if (input.QuantityShipped > remainingToShip)
                throw new ApiException(ErrorCodes.InternalOrderOverShipmentNotAllowed, $"Cannot ship {input.QuantityShipped} for article '{line.ArticleName}' — only {remainingToShip} remains to ship.", 400);

            var currentQuantity = stockByArticle.GetValueOrDefault(line.ArticleId, 0m);
            if (currentQuantity - input.QuantityShipped < 0)
                throw new ApiException(ErrorCodes.InternalOrderInsufficientStock, $"Cannot ship {input.QuantityShipped} of '{line.ArticleName}' — only {currentQuantity} available at the source warehouse.", 400);

            validatedLines.Add(new ValidatedShipmentLine { Line = line, QuantityShipped = input.QuantityShipped, Notes = input.Notes });
        }

        var actor = context.ActorUserToken.ToString();

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var headerParams = new DynamicParameters();
            headerParams.Add("@InternalOrderShipmentToken", Guid.NewGuid());
            headerParams.Add("@InternalOrderId", header.InternalOrderId);
            headerParams.Add("@SourceWarehouseId", sourceWarehouse.WarehouseId);
            headerParams.Add("@Notes", notes);
            headerParams.Add("@CreatedBy", actor);

            var shipmentHeader = await connection.QueryFirstOrDefaultAsync<InternalOrderShipment>(
                "sp_InternalOrderShipment_Create", headerParams, transaction, commandType: CommandType.StoredProcedure);

            if (shipmentHeader is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            foreach (var validated in validatedLines)
            {
                var lineParams = new DynamicParameters();
                lineParams.Add("@InternalOrderShipmentLineToken", Guid.NewGuid());
                lineParams.Add("@InternalOrderShipmentId", shipmentHeader.InternalOrderShipmentId);
                lineParams.Add("@InternalOrderLineId", validated.Line.InternalOrderLineId);
                lineParams.Add("@QuantityShipped", validated.QuantityShipped);
                lineParams.Add("@Notes", validated.Notes);
                lineParams.Add("@CreatedBy", actor);

                var shipmentLine = await connection.QueryFirstOrDefaultAsync<InternalOrderShipmentLine>(
                    "sp_InternalOrderShipmentLine_Create", lineParams, transaction, commandType: CommandType.StoredProcedure);

                if (shipmentLine is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }

                // Stock-out effect — the INTERNAL_ORDER_OUT trigger Inventory's own module now
                // has, mirroring InventoryTransfer's TRANSFER_OUT half exactly.
                await connection.ExecuteAsync(
                    "sp_StockLevel_ApplyDelta",
                    new { sourceWarehouse.WarehouseId, validated.Line.ArticleId, Delta = -validated.QuantityShipped, ActorBy = actor },
                    transaction, commandType: CommandType.StoredProcedure);

                var movementParams = new DynamicParameters();
                movementParams.Add("@InventoryMovementToken", Guid.NewGuid());
                movementParams.Add("@WarehouseId", sourceWarehouse.WarehouseId);
                movementParams.Add("@ArticleId", validated.Line.ArticleId);
                movementParams.Add("@Type", InventoryMovementTypeCodes.InternalOrderOut);
                movementParams.Add("@Quantity", -validated.QuantityShipped);
                movementParams.Add("@InternalOrderShipmentLineId", shipmentLine.InternalOrderShipmentLineId);
                movementParams.Add("@Reason", (string?)null);
                movementParams.Add("@CreatedBy", actor);
                await connection.ExecuteAsync("sp_InventoryMovement_Create", movementParams, transaction, commandType: CommandType.StoredProcedure);
            }

            // A first shipment moves REQUESTED -> SHIPPED. A later shipment against an order
            // already SHIPPED/PARTIALLY_RECEIVED never regresses the status — receiving (not
            // shipping) is what drives PARTIALLY_RECEIVED/RECEIVED, same split of responsibility
            // as PurchaseOrder's own Sent-vs-Received status halves.
            if (header.Status == InternalOrderStatusCodes.Requested)
            {
                await connection.ExecuteAsync(
                    "sp_InternalOrder_SetStatus",
                    new { InternalOrderToken = internalOrderToken, Status = InternalOrderStatusCodes.Shipped },
                    transaction, commandType: CommandType.StoredProcedure);
            }

            await transaction.CommitAsync(cancellationToken);

            // NotifyRequestingOrganizationAsync closes/reopens this connection — the committed
            // transaction must be disposed first, or SqlClient throws "The transaction associated
            // with the current connection has completed but has not been disposed" on that
            // Close(), silently swallowed by the notify helper's own try/catch, leaving the pooled
            // connection broken for whichever test/request reuses it next.
            await transaction.DisposeAsync();

            await NotifyRequestingOrganizationAsync(
                connection, header, NotificationType.Internal_Order_Shipped,
                new { internalOrderNumber = header.InternalOrderNumber },
                context, cancellationToken);

            var dto = mapper.Map<InternalOrderShipmentDto>(shipmentHeader);
            dto.Lines = mapper.MapList<InternalOrderShipmentLineDto>(
                await connection.QueryAsync<InternalOrderShipmentLine>(
                    "sp_InternalOrderShipmentLine_GetByInternalOrderShipmentId", new { shipmentHeader.InternalOrderShipmentId }, commandType: CommandType.StoredProcedure));

            return dto;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<InternalOrderReceiptDto?> CreateReceiptAsync(Guid internalOrderToken, string? notes, List<CreateInternalOrderReceiptLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var header = await connection.QueryFirstOrDefaultAsync<InternalOrder>(
            "sp_InternalOrder_GetByToken", new { InternalOrderToken = internalOrderToken }, commandType: CommandType.StoredProcedure);
        if (header is null)
            return null;

        // Receiving happens at the requesting Organization's own dock — the source Organization
        // has no business confirming what arrived at a warehouse it doesn't operate, same
        // access-boundary reasoning as PurchaseOrderService.CreateGoodsReceiptAsync's own
        // supplier-bypass note.
        if (!await CanManageOrganizationAsync(connection, context, header.RequestingOrganizationId, header.DestinationWarehouseId))
            throw new ApiException(ErrorCodes.InternalOrderForbidden, "Cannot record a receipt for an internal order outside your scope.", 403);

        if (header.Status is InternalOrderStatusCodes.Requested or InternalOrderStatusCodes.Cancelled or InternalOrderStatusCodes.Received)
            throw new ApiException(ErrorCodes.InternalOrderNotReceivable, "Only a shipped or partially received internal order can receive goods.", 409);

        var destinationWarehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = header.DestinationWarehouseToken }, commandType: CommandType.StoredProcedure);
        if (destinationWarehouse is null || !destinationWarehouse.CanReceivePurchases)
            throw new ApiException(ErrorCodes.InternalOrderReceiptWarehouseCannotReceive, "This warehouse is not configured to receive purchases.", 400);

        if (lines.Count == 0)
            throw new ApiException(ErrorCodes.InternalOrderReceiptEmpty, "At least one line must be received.", 400);

        var orderLines = (await connection.QueryAsync<InternalOrderLine>(
            "sp_InternalOrderLine_GetByInternalOrderId", new { header.InternalOrderId }, commandType: CommandType.StoredProcedure))
            .ToDictionary(l => l.InternalOrderLineId);

        var shipmentLines = (await connection.QueryAsync<InternalOrderShipmentLine>(
            "sp_InternalOrderShipmentLine_GetByInternalOrderId", new { header.InternalOrderId }, commandType: CommandType.StoredProcedure))
            .ToDictionary(l => l.InternalOrderShipmentLineToken);

        var validatedLines = new List<ValidatedReceiptLine>();
        var requestedShipmentLineIds = new HashSet<int>();

        foreach (var input in lines)
        {
            if (!shipmentLines.TryGetValue(input.InternalOrderShipmentLineToken, out var shipmentLine))
                throw new ApiException(ErrorCodes.InternalOrderReceiptLineNotFound, $"Shipment line '{input.InternalOrderShipmentLineToken}' does not belong to this internal order.", 404);

            if (!requestedShipmentLineIds.Add(shipmentLine.InternalOrderShipmentLineId))
                throw new ApiException(ErrorCodes.InternalOrderDuplicateLine, $"Article '{shipmentLine.ArticleName}' was submitted more than once.", 400);

            if (input.QuantityAccepted < 0 || input.QuantityRejected < 0)
                throw new ApiException(ErrorCodes.InternalOrderReceiptLineEmpty, $"Quantities for article '{shipmentLine.ArticleName}' cannot be negative.", 400);

            if (input.QuantityAccepted + input.QuantityRejected <= 0)
                throw new ApiException(ErrorCodes.InternalOrderReceiptLineEmpty, $"At least one quantity must be greater than zero for article '{shipmentLine.ArticleName}'.", 400);

            // 2-way split only — no Courtesy bucket, so both quantities together are capped
            // against what's still outstanding on this specific shipment line.
            var remaining = shipmentLine.QuantityShipped - (shipmentLine.QuantityAccepted + shipmentLine.QuantityRejected);
            if (input.QuantityAccepted + input.QuantityRejected > remaining)
                throw new ApiException(ErrorCodes.InternalOrderOverReceiptNotAllowed, $"Cannot receive {input.QuantityAccepted + input.QuantityRejected} for article '{shipmentLine.ArticleName}' — only {remaining} remains to receive.", 400);

            if (input.QuantityRejected > 0 && string.IsNullOrWhiteSpace(input.RejectionReason))
                throw new ApiException(ErrorCodes.InternalOrderRejectionReasonRequired, $"A rejection reason is required for article '{shipmentLine.ArticleName}'.", 400);

            if (!orderLines.TryGetValue(shipmentLine.InternalOrderLineId, out var orderLine))
                throw new ApiException(ErrorCodes.InternalOrderReceiptLineNotFound, $"Internal order line for article '{shipmentLine.ArticleName}' not found.", 404);

            validatedLines.Add(new ValidatedReceiptLine
            {
                ShipmentLine = shipmentLine,
                UnitPrice = orderLine.UnitPrice,
                QuantityAccepted = input.QuantityAccepted,
                QuantityRejected = input.QuantityRejected,
                RejectionReason = input.RejectionReason,
                Notes = input.Notes
            });
        }

        // Tax is computed only against the billable quantity (QuantityAccepted) — Rejected never
        // touches billing at all, same split GoodsReceiptLine's own tax computation uses.
        var taxByShipmentLineId = new Dictionary<int, InternalOrderLineTax>();
        var billableLines = validatedLines.Where(v => v.QuantityAccepted > 0).ToList();
        if (billableLines.Count > 0)
        {
            if (!destinationWarehouse.TaxJurisdictionId.HasValue)
                throw new ApiException(ErrorCodes.InternalOrderReceiptWarehouseTaxJurisdictionMissing, "This warehouse has no tax jurisdiction configured — set one before receiving billable goods.", 400);

            var distinctArticleIds = billableLines.Select(v => v.ShipmentLine.ArticleId).Distinct().ToList();
            var effectiveCategories = (await connection.QueryAsync<ArticleEffectiveTaxCategory>(
                "sp_Article_GetEffectiveTaxCategoryByIds",
                new { ArticleIds = string.Join(",", distinctArticleIds), destinationWarehouse.TaxJurisdictionId },
                commandType: CommandType.StoredProcedure)).ToDictionary(a => a.ArticleId);

            var rates = (await connection.QueryAsync<TaxRate>(
                "sp_TaxRate_GetByJurisdictionId",
                new { destinationWarehouse.TaxJurisdictionId },
                commandType: CommandType.StoredProcedure)).ToDictionary(r => r.TaxCategoryId);

            foreach (var validated in billableLines)
            {
                if (!effectiveCategories.TryGetValue(validated.ShipmentLine.ArticleId, out var effective) || !effective.TaxCategoryId.HasValue)
                    throw new ApiException(ErrorCodes.InternalOrderReceiptArticleTaxCategoryMissing, $"Article '{validated.ShipmentLine.ArticleName}' has no tax category configured (directly or via its Family) — configure one before receiving billable goods.", 400);

                if (!rates.TryGetValue(effective.TaxCategoryId.Value, out var rate))
                    throw new ApiException(ErrorCodes.InternalOrderReceiptTaxRateMissing, $"No tax rate is configured for category '{effective.TaxCategoryCode}' in this warehouse's tax jurisdiction.", 400);

                var taxableAmount = validated.QuantityAccepted * validated.UnitPrice;
                var taxAmount = Math.Round(taxableAmount * rate.RatePercent / 100m, 8);

                taxByShipmentLineId[validated.ShipmentLine.InternalOrderShipmentLineId] = new InternalOrderLineTax
                {
                    TaxCategoryId = effective.TaxCategoryId.Value,
                    TaxRateId = rate.TaxRateId,
                    TaxRatePercent = rate.RatePercent,
                    TaxableAmount = taxableAmount,
                    TaxAmount = taxAmount,
                    TotalAmount = taxableAmount + taxAmount
                };
            }
        }

        // "Fully received" is decided against the ORIGINAL InternalOrderLine.Quantity (not the
        // per-shipment-line remaining), same reasoning PurchaseOrder's own status recompute uses
        // against PurchaseOrderLine.Quantity — a Rejected quantity simply never counts toward
        // completion, same accepted simplification GoodsReceipt's own status recompute makes.
        var acceptedByOrderLineId = validatedLines
            .GroupBy(v => v.ShipmentLine.InternalOrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(v => v.QuantityAccepted));

        var everyLineFullyAccepted = orderLines.Values.All(l =>
            l.QuantityAccepted + acceptedByOrderLineId.GetValueOrDefault(l.InternalOrderLineId) >= l.Quantity);
        var anyLineAccepted = orderLines.Values.Any(l =>
            l.QuantityAccepted + acceptedByOrderLineId.GetValueOrDefault(l.InternalOrderLineId) > 0);

        var newStatus = everyLineFullyAccepted
            ? InternalOrderStatusCodes.Received
            : anyLineAccepted
                ? InternalOrderStatusCodes.PartiallyReceived
                : header.Status;

        var actor = context.ActorUserToken.ToString();

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var headerParams = new DynamicParameters();
            headerParams.Add("@InternalOrderReceiptToken", Guid.NewGuid());
            headerParams.Add("@InternalOrderId", header.InternalOrderId);
            headerParams.Add("@Notes", notes);
            headerParams.Add("@CreatedBy", actor);

            var receiptHeader = await connection.QueryFirstOrDefaultAsync<InternalOrderReceipt>(
                "sp_InternalOrderReceipt_Create", headerParams, transaction, commandType: CommandType.StoredProcedure);

            if (receiptHeader is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            foreach (var validated in validatedLines)
            {
                var tax = taxByShipmentLineId.GetValueOrDefault(validated.ShipmentLine.InternalOrderShipmentLineId);

                var lineParams = new DynamicParameters();
                lineParams.Add("@InternalOrderReceiptLineToken", Guid.NewGuid());
                lineParams.Add("@InternalOrderReceiptId", receiptHeader.InternalOrderReceiptId);
                lineParams.Add("@InternalOrderShipmentLineId", validated.ShipmentLine.InternalOrderShipmentLineId);
                lineParams.Add("@QuantityAccepted", validated.QuantityAccepted);
                lineParams.Add("@QuantityRejected", validated.QuantityRejected);
                lineParams.Add("@RejectionReason", validated.RejectionReason);
                lineParams.Add("@TaxCategoryId", tax?.TaxCategoryId);
                lineParams.Add("@TaxRateId", tax?.TaxRateId);
                lineParams.Add("@TaxRatePercent", tax?.TaxRatePercent);
                lineParams.Add("@TaxableAmount", tax?.TaxableAmount);
                lineParams.Add("@TaxAmount", tax?.TaxAmount);
                lineParams.Add("@TotalAmount", tax?.TotalAmount);
                lineParams.Add("@Notes", validated.Notes);
                lineParams.Add("@CreatedBy", actor);

                var receiptLine = await connection.QueryFirstOrDefaultAsync<InternalOrderReceiptLine>(
                    "sp_InternalOrderReceiptLine_Create", lineParams, transaction, commandType: CommandType.StoredProcedure);

                if (receiptLine is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }

                // Stock-in effect — only Accepted is real usable stock; Rejected never touches
                // it (no Courtesy bucket exists for an internal transfer, see the schema
                // migration's header note). Skipped entirely when the warehouse doesn't track
                // inventory, same soft-skip GoodsReceipt's own RECEIPT trigger uses.
                if (destinationWarehouse.IsInventoriable && validated.QuantityAccepted > 0)
                {
                    await connection.ExecuteAsync(
                        "sp_StockLevel_ApplyDelta",
                        new { destinationWarehouse.WarehouseId, validated.ShipmentLine.ArticleId, Delta = validated.QuantityAccepted, ActorBy = actor },
                        transaction, commandType: CommandType.StoredProcedure);

                    var movementParams = new DynamicParameters();
                    movementParams.Add("@InventoryMovementToken", Guid.NewGuid());
                    movementParams.Add("@WarehouseId", destinationWarehouse.WarehouseId);
                    movementParams.Add("@ArticleId", validated.ShipmentLine.ArticleId);
                    movementParams.Add("@Type", InventoryMovementTypeCodes.InternalOrderIn);
                    movementParams.Add("@Quantity", validated.QuantityAccepted);
                    movementParams.Add("@InternalOrderReceiptLineId", receiptLine.InternalOrderReceiptLineId);
                    movementParams.Add("@Reason", (string?)null);
                    movementParams.Add("@CreatedBy", actor);
                    await connection.ExecuteAsync("sp_InventoryMovement_Create", movementParams, transaction, commandType: CommandType.StoredProcedure);
                }
            }

            if (newStatus != header.Status)
            {
                await connection.ExecuteAsync(
                    "sp_InternalOrder_SetStatus",
                    new { InternalOrderToken = internalOrderToken, Status = newStatus },
                    transaction, commandType: CommandType.StoredProcedure);
            }

            await transaction.CommitAsync(cancellationToken);

            // See the identical comment in CreateShipmentAsync above.
            await transaction.DisposeAsync();

            await NotifyRequestingOrganizationAsync(
                connection, header, NotificationType.Internal_Order_Received,
                new { internalOrderNumber = header.InternalOrderNumber },
                context, cancellationToken);

            var dto = mapper.Map<InternalOrderReceiptDto>(receiptHeader);
            dto.Lines = mapper.MapList<InternalOrderReceiptLineDto>(
                await connection.QueryAsync<InternalOrderReceiptLine>(
                    "sp_InternalOrderReceiptLine_GetByInternalOrderReceiptId", new { receiptHeader.InternalOrderReceiptId }, commandType: CommandType.StoredProcedure));

            return dto;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<OrganizationDto>> GetEligibleSourceOrganizationsAsync(IRequestContext context, CancellationToken cancellationToken)
    {
        if (!context.OrganizationId.HasValue)
            return [];

        await using var connection = connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<Organization>(
            "sp_Organization_GetPeerAssociates", new { OrganizationId = context.OrganizationId.Value }, commandType: CommandType.StoredProcedure);

        return mapper.MapList<OrganizationDto>(rows);
    }
}
