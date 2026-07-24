using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;
using System.Data.Common;

namespace InnNou.Infrastructure.Services;

public class PurchaseOrderService(IDbConnectionFactory connectionFactory, IMapper mapper) : IPurchaseOrderService
{
    private sealed class PurchaseOrderPageRow : PurchaseOrder { public int TotalCount { get; set; } }
    private sealed class GoodsReceiptPageRow : GoodsReceipt { public int TotalCount { get; set; } }

    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    // Read visibility, no RoleLevel floor — matches WarehouseService.CanManageReadAsync. The
    // owning Supplier branch is checked separately by callers before falling back to this.
    private static async Task<bool> CanReadOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return true;

        if (!context.OrganizationId.HasValue)
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    // Write visibility (Cancel/Rectify) — only a caller whose own organization is ASSOCIATE may
    // write; SuperAdmin (no organization of their own, unless impersonating) and SUPER_ASSOCIATE
    // are read-only, mirrors OrderService.CanManageOrganizationAsync.
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

    // Always resolves EFFECTIVE values (post any APPLIED rectification) — see
    // sp_PurchaseOrderLine_GetEffective. Narrowed to a single PurchaseOrder via @PurchaseOrderId;
    // @OrderId is still required by the SP (it scopes the "latest APPLIED rectification" lookup
    // exactly the same way regardless of caller, no behavioral difference here).
    private static async Task<List<PurchaseOrderLine>> GetLinesForPurchaseOrderAsync(IDbConnection connection, PurchaseOrder purchaseOrder)
    {
        var lines = await connection.QueryAsync<PurchaseOrderLine>(
            "sp_PurchaseOrderLine_GetEffective",
            new { purchaseOrder.OrderId, purchaseOrder.PurchaseOrderId },
            commandType: CommandType.StoredProcedure);
        return lines.ToList();
    }

    public async Task<PagedResult<PurchaseOrderDto>> GetPagedAsync(Guid? organizationToken, Guid? orderToken, string? status, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        // Purely an additional narrowing filter layered on top of the scope resolved below —
        // never widens what the caller could already see, same rule the RoleIds/OrganizationIds
        // multi-value filter on GetUsers established.
        int? orderId = null;
        if (orderToken.HasValue)
        {
            var order = await connection.QueryFirstOrDefaultAsync<Order>(
                "sp_Order_GetByToken", new { OrderToken = orderToken.Value }, commandType: CommandType.StoredProcedure);

            if (order is null)
                return new PagedResult<PurchaseOrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            orderId = order.OrderId;
        }

        int? rootOrganizationId = null;
        int? supplierId = null;

        if (context.SupplierId.HasValue)
        {
            supplierId = context.SupplierId.Value;
        }
        else if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            rootOrganizationId = null; // unrestricted
        }
        else if (organizationToken.HasValue)
        {
            var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                "sp_Organization_GetByToken",
                new { OrganizationToken = organizationToken.Value, RootOrganizationId = (int?)null },
                commandType: CommandType.StoredProcedure);

            if (organization is null || !await CanReadOrganizationAsync(connection, context, organization.OrganizationId))
                return new PagedResult<PurchaseOrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            rootOrganizationId = organization.OrganizationId;
        }
        else if (context.OrganizationId.HasValue)
        {
            rootOrganizationId = context.OrganizationId.Value;
        }
        else
        {
            return new PagedResult<PurchaseOrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
        }

        int? statusId = null;
        if (status is not null)
        {
            // An unrecognized status filter matches nothing rather than 500ing.
            if (!PurchaseOrderStatusCodes.TryFromCode(status, out var parsedStatus))
                return new PagedResult<PurchaseOrderDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
            statusId = (int)parsedStatus;
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@SupplierId", supplierId);
        p.Add("@OrderId", orderId);
        p.Add("@StatusId", statusId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<PurchaseOrderPageRow>(
            "sp_PurchaseOrder_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<PurchaseOrderDto>
        {
            Items = mapper.MapList<PurchaseOrderDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<PurchaseOrderDto?> GetByTokenAsync(Guid purchaseOrderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (purchaseOrder is null)
            return null;

        var canView = context.SupplierId.HasValue
            ? context.SupplierId.Value == purchaseOrder.SupplierId
            : await CanReadOrganizationAsync(connection, context, purchaseOrder.OrganizationId);

        if (!canView)
            return null;

        var dto = mapper.Map<PurchaseOrderDto>(purchaseOrder);
        dto.Lines = mapper.MapList<PurchaseOrderLineDto>(
            await GetLinesForPurchaseOrderAsync(connection, purchaseOrder));
        dto.LineCount = dto.Lines.Count;
        return dto;
    }

    public async Task<PurchaseOrderDto?> CancelAsync(Guid purchaseOrderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        var canManage = context.SupplierId.HasValue
            ? context.SupplierId.Value == existing.SupplierId
            : await CanManageOrganizationAsync(connection, context, existing.OrganizationId);

        if (!canManage)
            throw new ApiException(ErrorCodes.PurchaseOrderForbidden, "Cannot cancel a purchase order outside your scope.", 403);

        if (existing.Status != PurchaseOrderStatus.Sent)
            throw new ApiException(ErrorCodes.PurchaseOrderNotSent, "Only a sent purchase order can be cancelled.", 409);

        var updated = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_Cancel",
            new { PurchaseOrderToken = purchaseOrderToken, CancelledBy = context.ActorUserToken.ToString() },
            commandType: CommandType.StoredProcedure);

        if (updated is null)
            return null;

        var dto = mapper.Map<PurchaseOrderDto>(updated);
        dto.Lines = mapper.MapList<PurchaseOrderLineDto>(
            await GetLinesForPurchaseOrderAsync(connection, updated));
        dto.LineCount = dto.Lines.Count;
        return dto;
    }

    private sealed class ValidatedRectificationLine
    {
        public required PurchaseOrderLine Line { get; init; }
        public required string Action { get; init; }
        public decimal? NewQuantity { get; init; }
        public decimal? NewUnitPrice { get; init; }
        public string? NewCurrencyCode { get; init; }
    }

    private sealed class TriggeredRectificationApprovalStep
    {
        public int FamilyId { get; set; }
        public string FamilyCode { get; set; } = default!;
        public int Level { get; set; }
        public decimal ThresholdAmount { get; set; }
        public decimal ActualFamilyAmount { get; set; }
        public int ApproverUserId { get; set; }
    }

    public async Task<PurchaseOrderRectificationDto?> CreateRectificationAsync(Guid purchaseOrderToken, string reason, string? notes, List<RectifyPurchaseOrderLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (purchaseOrder is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, purchaseOrder.OrganizationId))
            throw new ApiException(ErrorCodes.PurchaseOrderForbidden, "Cannot rectify a purchase order outside your scope.", 403);

        if (purchaseOrder.Status != PurchaseOrderStatus.Sent)
            throw new ApiException(ErrorCodes.PurchaseOrderNotSent, "Only a sent purchase order can be rectified.", 409);

        if (lines.Count == 0)
            throw new ApiException(ErrorCodes.PurchaseOrderRectificationEmpty, "At least one line must be rectified.", 400);

        if (!PurchaseOrderRectificationReasonCodes.TryFromCode(reason, out var normalizedReason))
            throw new ApiException(ErrorCodes.InvalidRequest, "Invalid rectification reason.", 400);

        // Effective lines across the WHOLE originating Order (every sibling PurchaseOrder) — needed
        // both to validate each requested line belongs to THIS PurchaseOrder and isn't already
        // cancelled, and to recompute each affected Family's total for the approval-threshold
        // check below against the same scope the original Submit evaluation used. See
        // .claude/PurchaseOrderRectificationModule.md.
        var allOrderLines = (await connection.QueryAsync<PurchaseOrderLine>(
            "sp_PurchaseOrderLine_GetEffective", new { purchaseOrder.OrderId, PurchaseOrderId = (int?)null }, commandType: CommandType.StoredProcedure)).ToList();

        var thisPoLinesByToken = allOrderLines
            .Where(l => l.PurchaseOrderId == purchaseOrder.PurchaseOrderId)
            .ToDictionary(l => l.PurchaseOrderLineToken);

        var validatedLines = new List<ValidatedRectificationLine>();

        foreach (var input in lines)
        {
            if (!thisPoLinesByToken.TryGetValue(input.PurchaseOrderLineToken, out var line))
                throw new ApiException(ErrorCodes.PurchaseOrderLineNotFound, $"Purchase order line '{input.PurchaseOrderLineToken}' does not belong to this purchase order.", 404);

            if (line.IsCancelled)
                throw new ApiException(ErrorCodes.PurchaseOrderLineAlreadyCancelled, $"The line for article '{line.ArticleName}' is already cancelled.", 409);

            if (input.Cancel)
            {
                validatedLines.Add(new ValidatedRectificationLine { Line = line, Action = PurchaseOrderRectificationLineActionCodes.LineCancelled });
                continue;
            }

            if (!input.NewQuantity.HasValue || input.NewQuantity.Value <= 0)
                throw new ApiException(ErrorCodes.PurchaseOrderRectificationInvalidQuantity, $"A positive NewQuantity is required for article '{line.ArticleName}'.", 400);
            if (!input.NewUnitPrice.HasValue || input.NewUnitPrice.Value <= 0)
                throw new ApiException(ErrorCodes.PurchaseOrderRectificationInvalidQuantity, $"A positive NewUnitPrice is required for article '{line.ArticleName}'.", 400);

            var newCurrencyCode = string.IsNullOrWhiteSpace(input.NewCurrencyCode) ? line.CurrencyCode : input.NewCurrencyCode.Trim().ToUpperInvariant();

            validatedLines.Add(new ValidatedRectificationLine
            {
                Line = line,
                Action = PurchaseOrderRectificationLineActionCodes.QuantityPriceChange,
                NewQuantity = input.NewQuantity,
                NewUnitPrice = input.NewUnitPrice,
                NewCurrencyCode = newCurrencyCode
            });
        }

        // Recompute each affected Family's total across the WHOLE Order using effective values,
        // with this rectification's proposed changes overlaid on top of the lines they touch —
        // same evaluation shape as OrderService.EvaluateApprovalRequirementAsync. Only levels not
        // already APPROVED for this (OrderId, FamilyId) trigger a fresh step — an earlier
        // Submit's or an earlier rectification's already-cleared levels stay cleared.
        var proposedByLineId = validatedLines.ToDictionary(v => v.Line.PurchaseOrderLineId);

        var currencyParams = new DynamicParameters();
        currencyParams.Add("@OrganizationId", purchaseOrder.OrganizationId);
        currencyParams.Add("@CurrencyCode", null, DbType.AnsiString, size: 10, direction: ParameterDirection.InputOutput);
        await connection.ExecuteAsync("sp_Organization_ResolveCurrencyCode", currencyParams, commandType: CommandType.StoredProcedure);
        var orgCurrencyCode = currencyParams.Get<string?>("@CurrencyCode");

        var familyTotals = new Dictionary<int, decimal>();
        if (orgCurrencyCode is not null)
        {
            foreach (var line in allOrderLines)
            {
                if (!line.FamilyId.HasValue)
                    continue;

                var isCancelled = line.IsCancelled;
                var quantity = line.Quantity;
                var unitPrice = line.UnitPrice;
                var currencyCode = line.CurrencyCode;

                if (proposedByLineId.TryGetValue(line.PurchaseOrderLineId, out var proposed))
                {
                    isCancelled = proposed.Action == PurchaseOrderRectificationLineActionCodes.LineCancelled;
                    if (!isCancelled)
                    {
                        quantity = proposed.NewQuantity!.Value;
                        unitPrice = proposed.NewUnitPrice!.Value;
                        currencyCode = proposed.NewCurrencyCode!;
                    }
                }

                if (isCancelled || currencyCode != orgCurrencyCode)
                    continue;

                familyTotals[line.FamilyId.Value] = familyTotals.GetValueOrDefault(line.FamilyId.Value) + quantity * unitPrice;
            }
        }

        var existingSteps = (await connection.QueryAsync<OrderApprovalStep>(
            "sp_OrderApprovalStep_GetByOrderId", new { purchaseOrder.OrderId }, commandType: CommandType.StoredProcedure)).ToList();

        var newSteps = new List<TriggeredRectificationApprovalStep>();

        foreach (var (familyId, total) in familyTotals)
        {
            var configuredLevels = (await connection.QueryAsync<FamilyApprovalThreshold>(
                "sp_FamilyApprovalThreshold_GetPaged",
                new { OrganizationId = purchaseOrder.OrganizationId, PageNumber = 1, PageSize = MaxPageSize, FamilyId = familyId, IncludeInactive = false },
                commandType: CommandType.StoredProcedure))
                .OrderBy(t => t.Level)
                .ToList();

            var highestTriggeredLevel = configuredLevels.Where(t => total >= t.ThresholdAmount).Select(t => (int?)t.Level).DefaultIfEmpty().Max();
            if (!highestTriggeredLevel.HasValue)
                continue;

            var alreadyApprovedLevels = existingSteps
                .Where(s => s.FamilyId == familyId && s.Status == OrderApprovalStepStatus.Approved)
                .Select(s => s.Level)
                .ToHashSet();

            foreach (var level in configuredLevels.Where(t => t.Level <= highestTriggeredLevel.Value && !alreadyApprovedLevels.Contains(t.Level)))
            {
                newSteps.Add(new TriggeredRectificationApprovalStep
                {
                    FamilyId = familyId,
                    FamilyCode = level.FamilyCode,
                    Level = level.Level,
                    ThresholdAmount = level.ThresholdAmount,
                    ActualFamilyAmount = total,
                    ApproverUserId = level.ApproverUserId
                });
            }
        }

        var needsApproval = newSteps.Count > 0;
        var initialStatus = needsApproval ? PurchaseOrderRectificationStatusCodes.PendingApproval : PurchaseOrderRectificationStatusCodes.Applied;
        var actor = context.ActorUserToken.ToString();

        // Header + lines (+ approval steps, if triggered) are inserted atomically — a partial
        // write here would leave an inconsistent rectification (e.g. a header with no lines).
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var headerParams = new DynamicParameters();
            headerParams.Add("@PurchaseOrderRectificationToken", Guid.NewGuid());
            headerParams.Add("@PurchaseOrderId", purchaseOrder.PurchaseOrderId);
            headerParams.Add("@Reason", normalizedReason);
            headerParams.Add("@Notes", notes);
            headerParams.Add("@Status", initialStatus);
            headerParams.Add("@CreatedBy", actor);

            var header = await connection.QueryFirstOrDefaultAsync<PurchaseOrderRectification>(
                "sp_PurchaseOrderRectification_Create", headerParams, transaction, commandType: CommandType.StoredProcedure);

            if (header is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            if (!needsApproval)
            {
                // Immediately applied — stamp AppliedUtc via the same SetStatus SP the
                // approved-later path uses, keeping one code path for "materialize this
                // rectification" regardless of whether approval was required.
                header = await connection.QueryFirstOrDefaultAsync<PurchaseOrderRectification>(
                    "sp_PurchaseOrderRectification_SetStatus",
                    new { header.PurchaseOrderRectificationId, Status = PurchaseOrderRectificationStatusCodes.Applied },
                    transaction, commandType: CommandType.StoredProcedure) ?? header;
            }

            foreach (var validated in validatedLines)
            {
                var lineParams = new DynamicParameters();
                lineParams.Add("@PurchaseOrderLineRectificationToken", Guid.NewGuid());
                lineParams.Add("@PurchaseOrderRectificationId", header.PurchaseOrderRectificationId);
                lineParams.Add("@PurchaseOrderLineId", validated.Line.PurchaseOrderLineId);
                lineParams.Add("@Action", validated.Action);
                lineParams.Add("@PreviousQuantity", validated.Line.Quantity);
                lineParams.Add("@NewQuantity", validated.NewQuantity);
                lineParams.Add("@PreviousUnitPrice", validated.Line.UnitPrice);
                lineParams.Add("@NewUnitPrice", validated.NewUnitPrice);
                lineParams.Add("@PreviousCurrencyCode", validated.Line.CurrencyCode);
                lineParams.Add("@NewCurrencyCode", validated.NewCurrencyCode);
                lineParams.Add("@CreatedBy", actor);

                await connection.ExecuteAsync("sp_PurchaseOrderLineRectification_Create", lineParams, transaction, commandType: CommandType.StoredProcedure);
            }

            if (needsApproval)
            {
                foreach (var step in newSteps)
                {
                    var stepParams = new DynamicParameters();
                    stepParams.Add("@OrderApprovalStepToken", Guid.NewGuid());
                    stepParams.Add("@OrderId", purchaseOrder.OrderId);
                    stepParams.Add("@FamilyId", step.FamilyId);
                    stepParams.Add("@FamilyCode", step.FamilyCode);
                    stepParams.Add("@Level", step.Level);
                    stepParams.Add("@ThresholdAmount", step.ThresholdAmount);
                    stepParams.Add("@ActualFamilyAmount", step.ActualFamilyAmount);
                    stepParams.Add("@CurrencyCode", orgCurrencyCode);
                    stepParams.Add("@ApproverUserId", step.ApproverUserId);
                    stepParams.Add("@CreatedBy", actor);
                    stepParams.Add("@TriggeringPurchaseOrderRectificationId", header.PurchaseOrderRectificationId);
                    await connection.ExecuteAsync("sp_OrderApprovalStep_Create", stepParams, transaction, commandType: CommandType.StoredProcedure);
                }
            }

            await transaction.CommitAsync(cancellationToken);

            var dto = mapper.Map<PurchaseOrderRectificationDto>(header);
            dto.Lines = mapper.MapList<PurchaseOrderLineRectificationDto>(
                await connection.QueryAsync<PurchaseOrderLineRectification>(
                    "sp_PurchaseOrderLineRectification_GetByRectificationId", new { header.PurchaseOrderRectificationId }, commandType: CommandType.StoredProcedure));

            return dto;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<PurchaseOrderRectificationDto>> GetRectificationsAsync(Guid purchaseOrderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (purchaseOrder is null)
            return [];

        var canView = context.SupplierId.HasValue
            ? context.SupplierId.Value == purchaseOrder.SupplierId
            : await CanReadOrganizationAsync(connection, context, purchaseOrder.OrganizationId);

        if (!canView)
            return [];

        var headers = (await connection.QueryAsync<PurchaseOrderRectification>(
            "sp_PurchaseOrderRectification_GetByPurchaseOrderId", new { purchaseOrder.PurchaseOrderId }, commandType: CommandType.StoredProcedure)).ToList();

        var result = new List<PurchaseOrderRectificationDto>();
        foreach (var header in headers)
        {
            var dto = mapper.Map<PurchaseOrderRectificationDto>(header);
            dto.Lines = mapper.MapList<PurchaseOrderLineRectificationDto>(
                await connection.QueryAsync<PurchaseOrderLineRectification>(
                    "sp_PurchaseOrderLineRectification_GetByRectificationId", new { header.PurchaseOrderRectificationId }, commandType: CommandType.StoredProcedure));
            result.Add(dto);
        }

        return result;
    }

    public async Task<PurchaseOrderRectificationDto?> GetRectificationByTokenAsync(Guid rectificationToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var header = await connection.QueryFirstOrDefaultAsync<PurchaseOrderRectification>(
            "sp_PurchaseOrderRectification_GetByToken", new { PurchaseOrderRectificationToken = rectificationToken }, commandType: CommandType.StoredProcedure);

        if (header is null)
            return null;

        var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { header.PurchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (purchaseOrder is null)
            return null;

        var canView = context.SupplierId.HasValue
            ? context.SupplierId.Value == purchaseOrder.SupplierId
            : await CanReadOrganizationAsync(connection, context, purchaseOrder.OrganizationId);

        if (!canView)
            return null;

        var dto = mapper.Map<PurchaseOrderRectificationDto>(header);
        dto.Lines = mapper.MapList<PurchaseOrderLineRectificationDto>(
            await connection.QueryAsync<PurchaseOrderLineRectification>(
                "sp_PurchaseOrderLineRectification_GetByRectificationId", new { header.PurchaseOrderRectificationId }, commandType: CommandType.StoredProcedure));

        return dto;
    }

    private sealed class ValidatedGoodsReceiptLine
    {
        public required PurchaseOrderLine Line { get; init; }
        public required CreateGoodsReceiptLineInputDto Input { get; init; }
    }

    public async Task<GoodsReceiptDto?> CreateGoodsReceiptAsync(Guid purchaseOrderToken, string? notes, List<CreateGoodsReceiptLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (purchaseOrder is null)
            return null;

        // Deliberately NOT the Cancel/Rectify supplier-bypass shape — receiving happens at the
        // buyer's own dock, performed by the buyer's staff. A supplier confirming what arrived
        // at a warehouse they don't operate would be a real access-boundary violation, not a
        // convenience.
        if (!await CanManageOrganizationAsync(connection, context, purchaseOrder.OrganizationId))
            throw new ApiException(ErrorCodes.GoodsReceiptForbidden, "Cannot record a goods receipt for a purchase order outside your scope.", 403);

        if (purchaseOrder.Status != PurchaseOrderStatus.Sent && purchaseOrder.Status != PurchaseOrderStatus.Partially_Received)
            throw new ApiException(ErrorCodes.GoodsReceiptPurchaseOrderNotReceivable, "Only a sent or partially received purchase order can receive goods.", 409);

        if (lines.Count == 0)
            throw new ApiException(ErrorCodes.GoodsReceiptEmpty, "At least one line must be received.", 400);

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { purchaseOrder.WarehouseToken }, commandType: CommandType.StoredProcedure);

        if (warehouse is null || !warehouse.CanReceivePurchases)
            throw new ApiException(ErrorCodes.GoodsReceiptWarehouseCannotReceive, "This warehouse is not configured to receive purchases.", 400);

        var effectiveLines = await GetLinesForPurchaseOrderAsync(connection, purchaseOrder);
        var linesByToken = effectiveLines.ToDictionary(l => l.PurchaseOrderLineToken);

        var existingReceiptLines = (await connection.QueryAsync<GoodsReceiptLine>(
            "sp_GoodsReceiptLine_GetByPurchaseOrderId", new { purchaseOrder.PurchaseOrderId }, commandType: CommandType.StoredProcedure)).ToList();

        var alreadyAccepted = existingReceiptLines
            .GroupBy(l => l.PurchaseOrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.QuantityAccepted));

        var validatedLines = new List<ValidatedGoodsReceiptLine>();
        var requestedLineIds = new HashSet<int>();

        foreach (var input in lines)
        {
            if (!linesByToken.TryGetValue(input.PurchaseOrderLineToken, out var line))
                throw new ApiException(ErrorCodes.GoodsReceiptLineNotFound, $"Purchase order line '{input.PurchaseOrderLineToken}' does not belong to this purchase order.", 404);

            if (!requestedLineIds.Add(line.PurchaseOrderLineId))
                throw new ApiException(ErrorCodes.GoodsReceiptLineNotFound, $"Purchase order line '{input.PurchaseOrderLineToken}' was submitted more than once.", 400);

            if (line.IsCancelled)
                throw new ApiException(ErrorCodes.GoodsReceiptLineAlreadyCancelled, $"The line for article '{line.ArticleName}' was cancelled by a rectification and cannot receive goods.", 409);

            if (input.QuantityAccepted < 0 || input.QuantityCourtesy < 0 || input.QuantityRejected < 0)
                throw new ApiException(ErrorCodes.GoodsReceiptLineEmpty, $"Quantities for article '{line.ArticleName}' cannot be negative.", 400);

            if (input.QuantityAccepted + input.QuantityCourtesy + input.QuantityRejected <= 0)
                throw new ApiException(ErrorCodes.GoodsReceiptLineEmpty, $"At least one quantity must be greater than zero for article '{line.ArticleName}'.", 400);

            var remaining = line.Quantity - alreadyAccepted.GetValueOrDefault(line.PurchaseOrderLineId);
            if (input.QuantityAccepted > remaining)
                throw new ApiException(ErrorCodes.GoodsReceiptOverReceiptNotAllowed, $"Cannot accept {input.QuantityAccepted} for article '{line.ArticleName}' — only {remaining} remains to receive. Any supplier surplus must be recorded as Courtesy or Rejected.", 400);

            if (input.QuantityAccepted > 0 && warehouse.TrackLotNumbers && string.IsNullOrWhiteSpace(input.LotNumber))
                throw new ApiException(ErrorCodes.GoodsReceiptLotNumberRequired, $"A lot number is required for article '{line.ArticleName}' at this warehouse.", 400);

            if (input.QuantityAccepted > 0 && warehouse.TrackExpirationDates && !input.ExpirationDate.HasValue)
                throw new ApiException(ErrorCodes.GoodsReceiptExpirationDateRequired, $"An expiration date is required for article '{line.ArticleName}' at this warehouse.", 400);

            if (input.QuantityAccepted > 0 && warehouse.TrackSerialNumbers && string.IsNullOrWhiteSpace(input.SerialNumber))
                throw new ApiException(ErrorCodes.GoodsReceiptSerialNumberRequired, $"A serial number is required for article '{line.ArticleName}' at this warehouse.", 400);

            if (input.QuantityRejected > 0 && string.IsNullOrWhiteSpace(input.RejectionReason))
                throw new ApiException(ErrorCodes.GoodsReceiptRejectionReasonRequired, $"A rejection reason is required for article '{line.ArticleName}'.", 400);

            validatedLines.Add(new ValidatedGoodsReceiptLine { Line = line, Input = input });
        }

        var acceptedByLineId = validatedLines.ToDictionary(v => v.Line.PurchaseOrderLineId, v => v.Input.QuantityAccepted);
        var everyLineFullyAccepted = effectiveLines
            .Where(l => !l.IsCancelled)
            .All(l => alreadyAccepted.GetValueOrDefault(l.PurchaseOrderLineId) + acceptedByLineId.GetValueOrDefault(l.PurchaseOrderLineId) >= l.Quantity);
        var anyLineAccepted = effectiveLines
            .Any(l => alreadyAccepted.GetValueOrDefault(l.PurchaseOrderLineId) + acceptedByLineId.GetValueOrDefault(l.PurchaseOrderLineId) > 0);

        var newStatus = everyLineFullyAccepted
            ? PurchaseOrderStatusCodes.Received
            : anyLineAccepted
                ? PurchaseOrderStatusCodes.PartiallyReceived
                : PurchaseOrderStatusCodes.Sent;

        var actor = context.ActorUserToken.ToString();

        // Header + lines + the PurchaseOrder status recompute are inserted/updated atomically —
        // a partial write here would leave a receipt whose lines don't match the PO's reported
        // status. Same shape as CreateRectificationAsync above.
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var headerParams = new DynamicParameters();
            headerParams.Add("@GoodsReceiptToken", Guid.NewGuid());
            headerParams.Add("@PurchaseOrderId", purchaseOrder.PurchaseOrderId);
            headerParams.Add("@WarehouseId", purchaseOrder.WarehouseId);
            headerParams.Add("@Notes", notes);
            headerParams.Add("@CreatedBy", actor);

            var header = await connection.QueryFirstOrDefaultAsync<GoodsReceipt>(
                "sp_GoodsReceipt_Create", headerParams, transaction, commandType: CommandType.StoredProcedure);

            if (header is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            foreach (var validated in validatedLines)
            {
                var lineParams = new DynamicParameters();
                lineParams.Add("@GoodsReceiptLineToken", Guid.NewGuid());
                lineParams.Add("@GoodsReceiptId", header.GoodsReceiptId);
                lineParams.Add("@PurchaseOrderLineId", validated.Line.PurchaseOrderLineId);
                lineParams.Add("@ArticleId", validated.Line.ArticleId);
                lineParams.Add("@QuantityAccepted", validated.Input.QuantityAccepted);
                lineParams.Add("@QuantityCourtesy", validated.Input.QuantityCourtesy);
                lineParams.Add("@QuantityRejected", validated.Input.QuantityRejected);
                lineParams.Add("@RejectionReason", validated.Input.RejectionReason);
                lineParams.Add("@LotNumber", validated.Input.LotNumber);
                lineParams.Add("@ExpirationDate", validated.Input.ExpirationDate);
                lineParams.Add("@SerialNumber", validated.Input.SerialNumber);
                lineParams.Add("@Notes", validated.Input.Notes);
                lineParams.Add("@CreatedBy", actor);

                var receiptLine = await connection.QueryFirstOrDefaultAsync<GoodsReceiptLine>(
                    "sp_GoodsReceiptLine_Create", lineParams, transaction, commandType: CommandType.StoredProcedure);

                // Stock effect — the RECEIPT trigger Inventory's own module doc describes. Both
                // Accepted and Courtesy are real physical stock regardless of billing; Rejected
                // never touches stock. Skipped entirely (not a failure) when the warehouse
                // doesn't track inventory — receiving is still recorded in GoodsReceipt itself,
                // stock tracking is an optional side effect. See .claude/InventoryModule.md.
                var stockDelta = validated.Input.QuantityAccepted + validated.Input.QuantityCourtesy;
                if (warehouse.IsInventoriable && stockDelta > 0 && receiptLine is not null)
                {
                    await connection.ExecuteAsync(
                        "sp_StockLevel_ApplyDelta",
                        new { warehouse.WarehouseId, validated.Line.ArticleId, Delta = stockDelta, ActorBy = actor },
                        transaction, commandType: CommandType.StoredProcedure);

                    var movementParams = new DynamicParameters();
                    movementParams.Add("@InventoryMovementToken", Guid.NewGuid());
                    movementParams.Add("@WarehouseId", warehouse.WarehouseId);
                    movementParams.Add("@ArticleId", validated.Line.ArticleId);
                    movementParams.Add("@Type", InventoryMovementTypeCodes.Receipt);
                    movementParams.Add("@Quantity", stockDelta);
                    movementParams.Add("@GoodsReceiptLineId", receiptLine.GoodsReceiptLineId);
                    movementParams.Add("@InventoryTransferLineId", (int?)null);
                    movementParams.Add("@Reason", (string?)null);
                    movementParams.Add("@CreatedBy", actor);

                    await connection.ExecuteAsync("sp_InventoryMovement_Create", movementParams, transaction, commandType: CommandType.StoredProcedure);
                }
            }

            await connection.ExecuteAsync(
                "sp_PurchaseOrder_SetStatus",
                new { PurchaseOrderToken = purchaseOrderToken, Status = newStatus },
                transaction, commandType: CommandType.StoredProcedure);

            await transaction.CommitAsync(cancellationToken);

            var dto = mapper.Map<GoodsReceiptDto>(header);
            dto.Lines = mapper.MapList<GoodsReceiptLineDto>(
                await connection.QueryAsync<GoodsReceiptLine>(
                    "sp_GoodsReceiptLine_GetByGoodsReceiptId", new { header.GoodsReceiptId }, commandType: CommandType.StoredProcedure));
            dto.LineCount = dto.Lines.Count;

            return dto;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PagedResult<GoodsReceiptDto>> GetGoodsReceiptsAsync(Guid? purchaseOrderToken, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        int? purchaseOrderId = null;
        int? rootOrganizationId = null;
        int? supplierId = null;

        if (purchaseOrderToken.HasValue)
        {
            var purchaseOrder = await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(
                "sp_PurchaseOrder_GetByToken", new { PurchaseOrderToken = purchaseOrderToken.Value }, commandType: CommandType.StoredProcedure);

            if (purchaseOrder is null)
                return new PagedResult<GoodsReceiptDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            var canView = context.SupplierId.HasValue
                ? context.SupplierId.Value == purchaseOrder.SupplierId
                : await CanReadOrganizationAsync(connection, context, purchaseOrder.OrganizationId);

            if (!canView)
                return new PagedResult<GoodsReceiptDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            purchaseOrderId = purchaseOrder.PurchaseOrderId;
        }
        else if (context.SupplierId.HasValue)
        {
            supplierId = context.SupplierId.Value;
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
            return new PagedResult<GoodsReceiptDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@SupplierId", supplierId);
        p.Add("@PurchaseOrderId", purchaseOrderId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<GoodsReceiptPageRow>(
            "sp_GoodsReceipt_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        var items = mapper.MapList<GoodsReceiptDto>(rows);

        // Unlike PurchaseOrder's own GetPagedAsync, hydrate Lines for every row here — a
        // GoodsReceipt list is always scoped to a handful of receipts for one PurchaseOrder
        // (never an unbounded cross-organization browse), and the caller (the "Receive" modal)
        // needs every line's QuantityAccepted to compute what's already been received.
        foreach (var item in items)
        {
            var goodsReceipt = rows.First(r => r.GoodsReceiptToken == item.GoodsReceiptToken);
            item.Lines = mapper.MapList<GoodsReceiptLineDto>(
                await connection.QueryAsync<GoodsReceiptLine>(
                    "sp_GoodsReceiptLine_GetByGoodsReceiptId", new { goodsReceipt.GoodsReceiptId }, commandType: CommandType.StoredProcedure));
        }

        return new PagedResult<GoodsReceiptDto>
        {
            Items = items,
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<GoodsReceiptDto?> GetGoodsReceiptByTokenAsync(Guid goodsReceiptToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var header = await connection.QueryFirstOrDefaultAsync<GoodsReceipt>(
            "sp_GoodsReceipt_GetByToken", new { GoodsReceiptToken = goodsReceiptToken }, commandType: CommandType.StoredProcedure);

        if (header is null)
            return null;

        var canView = context.SupplierId.HasValue
            ? context.SupplierId.Value == header.SupplierId
            : await CanReadOrganizationAsync(connection, context, header.OrganizationId);

        if (!canView)
            return null;

        var dto = mapper.Map<GoodsReceiptDto>(header);
        dto.Lines = mapper.MapList<GoodsReceiptLineDto>(
            await connection.QueryAsync<GoodsReceiptLine>(
                "sp_GoodsReceiptLine_GetByGoodsReceiptId", new { header.GoodsReceiptId }, commandType: CommandType.StoredProcedure));
        dto.LineCount = dto.Lines.Count;

        return dto;
    }
}
