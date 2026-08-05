namespace InnNou.Application.Common
{
    // A WarehouseContact's own login (real credentials, not impersonation — see
    // SupplierAccessModule/WarehousesModule's shadow-user mechanism) carries
    // IRequestContext.WarehouseId, scoping that identity to exactly one Warehouse. Every service
    // that resolves a caller-reachable Warehouse (directly, or via an Order/PurchaseOrder/etc.
    // that belongs to one) must layer this check on top of its existing organization-hierarchy
    // check — same "AND on top of the scope" placement as GetUsersQueryRequest's RoleIds/
    // OrganizationIds filters, never merged into the org check itself.
    public static class WarehouseScopeGuard
    {
        // context.WarehouseId is null for every non-warehouse-scoped caller (SuperAdmin, Admin,
        // Supervisor, Employee, Supplier) — those are unaffected. Only a WarehouseContact's own
        // identity has it set, and for them targetWarehouseId must match exactly.
        public static bool Allows(IRequestContext context, int? targetWarehouseId)
            => !context.WarehouseId.HasValue || context.WarehouseId.Value == targetWarehouseId;

        // For a two-warehouse operation (e.g. an Inventory Transfer) — a warehouse-scoped caller
        // must be on at least one side of it (sending stock out or receiving it in), not neither.
        public static bool AllowsEither(IRequestContext context, int sourceWarehouseId, int destinationWarehouseId)
            => !context.WarehouseId.HasValue
               || context.WarehouseId.Value == sourceWarehouseId
               || context.WarehouseId.Value == destinationWarehouseId;
    }
}
