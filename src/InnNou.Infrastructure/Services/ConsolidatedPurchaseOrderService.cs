using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Documents;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class ConsolidatedPurchaseOrderService(IDbConnectionFactory connectionFactory, IMapper mapper) : IConsolidatedPurchaseOrderService
{
    private sealed class ConsolidatedPurchaseOrderPageRow : ConsolidatedPurchaseOrder { public int TotalCount { get; set; } }

    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    // Resolves the SuperAssociateOrganizationId this call is scoped to. There is no "global"
    // concept here (unlike CategoryService) — a bare SuperAdmin must supply an explicit token;
    // a Staff+ caller whose own organization IS a SUPER_ASSOCIATE is forced to their own
    // context.OrganizationId, any different token they pass is ignored. Same shape as
    // CategoryService.ResolveWriteOwnerOrganizationIdAsync / ArticleClassificationService.
    private static async Task<int> ResolveSuperAssociateOrganizationIdAsync(IDbConnection connection, IRequestContext context, Guid? superAssociateOrganizationToken)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            if (!superAssociateOrganizationToken.HasValue)
                throw new ApiException(ErrorCodes.ConsolidatedPurchaseOrderForbidden, "SuperAssociateOrganizationToken is required.", 400);

            var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                "sp_Organization_GetByToken", new { OrganizationToken = superAssociateOrganizationToken.Value }, commandType: CommandType.StoredProcedure);

            if (organization is null)
                throw new ApiException(ErrorCodes.ConsolidatedPurchaseOrderOrganizationNotFound, "The specified organization was not found.", 404);

            if (organization.OrganizationTypeCode != OrganizationTypeCodes.SuperAssociate)
                throw new ApiException(ErrorCodes.ConsolidatedPurchaseOrderForbidden, "Consolidations can only be created for a Super Asociado organization.", 403);

            return organization.OrganizationId;
        }

        if (context.OrganizationTypeCode == OrganizationTypeCodes.SuperAssociate
            && context.RoleLevel >= StaffRoleLevel
            && context.OrganizationId.HasValue)
        {
            return context.OrganizationId.Value;
        }

        throw new ApiException(ErrorCodes.ConsolidatedPurchaseOrderForbidden, "Insufficient permissions to manage purchase order consolidations.", 403);
    }

    // Visible only to the owning Super Asociado (or SuperAdmin) — never to the individual
    // properties whose PurchaseOrders were pulled in. This is a group-level negotiation tool.
    private static bool CanView(IRequestContext context, int superAssociateOrganizationId)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return true;

        return context.OrganizationTypeCode == OrganizationTypeCodes.SuperAssociate
            && context.RoleLevel >= StaffRoleLevel
            && context.OrganizationId == superAssociateOrganizationId;
    }

    private static async Task<List<PurchaseOrder>> GetCandidatesInternalAsync(IDbConnection connection, int supplierId, int superAssociateOrganizationId, DateTime dateFrom, DateTime dateTo)
    {
        var p = new DynamicParameters();
        p.Add("@SupplierId", supplierId);
        p.Add("@SuperAssociateOrganizationId", superAssociateOrganizationId);
        p.Add("@DateFrom", dateFrom.Date);
        p.Add("@DateTo", dateTo.Date);

        var rows = await connection.QueryAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetCandidatesForConsolidation", p, commandType: CommandType.StoredProcedure);
        return rows.ToList();
    }

    private static async Task<ConsolidatedPurchaseOrderDto> HydrateMembersAsync(IDbConnection connection, ConsolidatedPurchaseOrder header, IMapper mapper)
    {
        var dto = mapper.Map<ConsolidatedPurchaseOrderDto>(header);
        dto.Members = mapper.MapList<ConsolidatedPurchaseOrderMemberDto>(
            await connection.QueryAsync<ConsolidatedPurchaseOrderMember>(
                "sp_ConsolidatedPurchaseOrderMember_GetByConsolidatedPurchaseOrderId", new { header.ConsolidatedPurchaseOrderId }, commandType: CommandType.StoredProcedure));
        dto.MemberCount = dto.Members.Count;
        return dto;
    }

    public async Task<List<PurchaseOrderDto>> GetCandidatesAsync(Guid supplierToken, Guid? superAssociateOrganizationToken, DateTime dateFrom, DateTime dateTo, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var superAssociateOrganizationId = await ResolveSuperAssociateOrganizationIdAsync(connection, context, superAssociateOrganizationToken);

        if (dateTo.Date < dateFrom.Date)
            throw new ApiException(ErrorCodes.ConsolidatedPurchaseOrderInvalidDateRange, "DateRangeTo must be on or after DateRangeFrom.", 400);

        var supplier = await connection.QueryFirstOrDefaultAsync<Supplier>(
            "sp_Supplier_GetByToken", new { SupplierToken = supplierToken }, commandType: CommandType.StoredProcedure);
        if (supplier is null)
            throw new ApiException(ErrorCodes.SupplierNotFound, "Supplier not found.", 404);

        var candidates = await GetCandidatesInternalAsync(connection, supplier.SupplierId, superAssociateOrganizationId, dateFrom, dateTo);
        return mapper.MapList<PurchaseOrderDto>(candidates);
    }

    public async Task<ConsolidatedPurchaseOrderDto?> CreateAsync(Guid supplierToken, Guid? superAssociateOrganizationToken, string? title, string? notes, DateTime dateFrom, DateTime dateTo, List<Guid> purchaseOrderTokens, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var superAssociateOrganizationId = await ResolveSuperAssociateOrganizationIdAsync(connection, context, superAssociateOrganizationToken);

        if (dateTo.Date < dateFrom.Date)
            throw new ApiException(ErrorCodes.ConsolidatedPurchaseOrderInvalidDateRange, "DateRangeTo must be on or after DateRangeFrom.", 400);

        var supplier = await connection.QueryFirstOrDefaultAsync<Supplier>(
            "sp_Supplier_GetByToken", new { SupplierToken = supplierToken }, commandType: CommandType.StoredProcedure);
        if (supplier is null)
            throw new ApiException(ErrorCodes.SupplierNotFound, "Supplier not found.", 404);

        if (purchaseOrderTokens.Count == 0)
            throw new ApiException(ErrorCodes.ConsolidatedPurchaseOrderEmpty, "At least one purchase order must be selected.", 400);

        // Re-derive the exact candidates list rather than trusting client-supplied tokens
        // blindly — guarantees every selected PurchaseOrder really belongs to this Supplier,
        // this Super Asociado's hierarchy, this date range, and isn't already claimed by
        // another consolidation.
        var candidates = await GetCandidatesInternalAsync(connection, supplier.SupplierId, superAssociateOrganizationId, dateFrom, dateTo);
        var candidatesByToken = candidates.ToDictionary(c => c.PurchaseOrderToken);

        var selectedPurchaseOrders = new List<PurchaseOrder>();
        foreach (var token in purchaseOrderTokens)
        {
            if (!candidatesByToken.TryGetValue(token, out var candidate))
                throw new ApiException(ErrorCodes.ConsolidatedPurchaseOrderInvalidMember, $"Purchase order '{token}' is not a valid candidate for this consolidation.", 400);

            selectedPurchaseOrders.Add(candidate);
        }

        var actor = context.ActorUserToken.ToString();

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var headerParams = new DynamicParameters();
            headerParams.Add("@ConsolidatedPurchaseOrderToken", Guid.NewGuid());
            headerParams.Add("@SupplierId", supplier.SupplierId);
            headerParams.Add("@SuperAssociateOrganizationId", superAssociateOrganizationId);
            headerParams.Add("@Title", string.IsNullOrWhiteSpace(title) ? null : title.Trim());
            headerParams.Add("@Notes", string.IsNullOrWhiteSpace(notes) ? null : notes.Trim());
            headerParams.Add("@DateRangeFrom", dateFrom.Date);
            headerParams.Add("@DateRangeTo", dateTo.Date);
            headerParams.Add("@CreatedBy", actor);

            var header = await connection.QueryFirstOrDefaultAsync<ConsolidatedPurchaseOrder>(
                "sp_ConsolidatedPurchaseOrder_Create", headerParams, transaction, commandType: CommandType.StoredProcedure);

            if (header is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            foreach (var po in selectedPurchaseOrders)
            {
                var memberParams = new DynamicParameters();
                memberParams.Add("@ConsolidatedPurchaseOrderId", header.ConsolidatedPurchaseOrderId);
                memberParams.Add("@PurchaseOrderId", po.PurchaseOrderId);
                memberParams.Add("@CreatedBy", actor);
                await connection.ExecuteAsync("sp_ConsolidatedPurchaseOrderMember_Create", memberParams, transaction, commandType: CommandType.StoredProcedure);
            }

            await transaction.CommitAsync(cancellationToken);

            return await HydrateMembersAsync(connection, header, mapper);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PagedResult<ConsolidatedPurchaseOrderDto>> GetPagedAsync(Guid? superAssociateOrganizationToken, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        var superAssociateOrganizationId = await ResolveSuperAssociateOrganizationIdAsync(connection, context, superAssociateOrganizationToken);

        var p = new DynamicParameters();
        p.Add("@SuperAssociateOrganizationId", superAssociateOrganizationId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<ConsolidatedPurchaseOrderPageRow>(
            "sp_ConsolidatedPurchaseOrder_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<ConsolidatedPurchaseOrderDto>
        {
            Items = mapper.MapList<ConsolidatedPurchaseOrderDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<ConsolidatedPurchaseOrderDto?> GetByTokenAsync(Guid consolidatedPurchaseOrderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var header = await connection.QueryFirstOrDefaultAsync<ConsolidatedPurchaseOrder>(
            "sp_ConsolidatedPurchaseOrder_GetByToken", new { ConsolidatedPurchaseOrderToken = consolidatedPurchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (header is null || !CanView(context, header.SuperAssociateOrganizationId))
            return null;

        return await HydrateMembersAsync(connection, header, mapper);
    }

    public async Task<bool> DeleteAsync(Guid consolidatedPurchaseOrderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var header = await connection.QueryFirstOrDefaultAsync<ConsolidatedPurchaseOrder>(
            "sp_ConsolidatedPurchaseOrder_GetByToken", new { ConsolidatedPurchaseOrderToken = consolidatedPurchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (header is null)
            return false;

        if (!CanView(context, header.SuperAssociateOrganizationId))
            throw new ApiException(ErrorCodes.ConsolidatedPurchaseOrderForbidden, "Cannot delete a consolidation outside your organization's scope.", 403);

        await connection.ExecuteAsync(
            "sp_ConsolidatedPurchaseOrder_Delete", new { ConsolidatedPurchaseOrderToken = consolidatedPurchaseOrderToken }, commandType: CommandType.StoredProcedure);

        return true;
    }

    public async Task<(byte[] FileBytes, string FileName)?> GetPdfAsync(Guid consolidatedPurchaseOrderToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var header = await connection.QueryFirstOrDefaultAsync<ConsolidatedPurchaseOrder>(
            "sp_ConsolidatedPurchaseOrder_GetByToken", new { ConsolidatedPurchaseOrderToken = consolidatedPurchaseOrderToken }, commandType: CommandType.StoredProcedure);

        if (header is null)
            return null;

        if (!CanView(context, header.SuperAssociateOrganizationId))
            throw new ApiException(ErrorCodes.ConsolidatedPurchaseOrderForbidden, "Cannot access a consolidation outside your organization's scope.", 403);

        var members = (await connection.QueryAsync<ConsolidatedPurchaseOrderMember>(
            "sp_ConsolidatedPurchaseOrderMember_GetByConsolidatedPurchaseOrderId", new { header.ConsolidatedPurchaseOrderId }, commandType: CommandType.StoredProcedure)).ToList();

        var memberLines = new List<(ConsolidatedPurchaseOrderMember Member, List<PurchaseOrderLine> Lines)>();
        foreach (var member in members)
        {
            var lines = (await connection.QueryAsync<PurchaseOrderLine>(
                "sp_PurchaseOrderLine_GetEffective", new { member.OrderId, member.PurchaseOrderId }, commandType: CommandType.StoredProcedure)).ToList();
            memberLines.Add((member, lines));
        }

        var pdfBytes = ConsolidatedPurchaseOrderDocument.Build(header, memberLines);
        var fileName = $"consolidated-po-{header.ConsolidatedPurchaseOrderToken:N}.pdf";
        return (pdfBytes, fileName);
    }
}
