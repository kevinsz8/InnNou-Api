using FluentAssertions;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.IntegrationTests.TestFixtures;
using Xunit;

namespace InnNou.IntegrationTests.WarehouseContacts;

/// <summary>
/// Regression coverage for a gap found in the 2026-08-07 full-system audit:
/// WarehouseContactService's CanManageOrganizationAsync/CanManageReadAsync only checked
/// organization-hierarchy, never IRequestContext.WarehouseId — so a real WarehouseContact login
/// (RoleLevel 20, WarehouseId scoped to exactly one Warehouse) could reach ANY other warehouse's
/// contacts within the same organization. Same bug class already fixed once before in
/// InventoryService (see memory project_warehouse_contact_scope_gap, 2026-08-05); this is a second,
/// independent instance in a sibling service. Fixed by layering WarehouseScopeGuard.Allows on top
/// of the existing org-hierarchy check, same pattern InventoryService/ParLevelService/etc. already
/// use — see CLAUDE.md's IRequestContext.WarehouseId note.
/// </summary>
public class WarehouseContactScopeTests(DatabaseFixture fixture) : TransactionalTestBase(fixture)
{
    private async Task<(int OrganizationId, Guid WarehouseTokenA, int WarehouseIdA, Guid WarehouseTokenB)> SetupTwoWarehousesAsync(string namePrefix)
    {
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        var esJurisdiction = await Data.GetTaxJurisdictionTokenAsync("ES_MAINLAND_BALEARIC");

        Context.RoleLevel = 80;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";

        var warehouseTokenA = await Data.CreateWarehouseAsync(organizationToken, esJurisdiction, $"{namePrefix} A");
        var warehouseTokenB = await Data.CreateWarehouseAsync(organizationToken, esJurisdiction, $"{namePrefix} B");
        var warehouseIdA = await Data.GetWarehouseIdAsync(warehouseTokenA);

        return (organizationId, warehouseTokenA, warehouseIdA, warehouseTokenB);
    }

    // Simulates a real WarehouseContact's own login — RoleLevel 20, WarehouseId set to exactly one
    // Warehouse — as opposed to every other test context in this suite (SuperAdmin/Admin), which
    // never has WarehouseId set and is therefore unaffected by WarehouseScopeGuard by design.
    private void ActAsWarehouseContact(int organizationId, int warehouseId)
    {
        Context.RoleLevel = 20;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";
        Context.WarehouseId = warehouseId;
    }

    [Fact]
    public async Task CreateAsync_ForAWarehouseOutsideTheCallersScope_IsForbidden()
    {
        var (organizationId, warehouseTokenA, warehouseIdA, warehouseTokenB) = await SetupTwoWarehousesAsync("WHCONTACT_SCOPE_CREATE");
        ActAsWarehouseContact(organizationId, warehouseIdA);

        var result = await Mediator.Send(new CreateWarehouseContactCommandRequest
        {
            WarehouseToken = warehouseTokenB,
            ContactName = "Should Be Blocked",
            IsPrimary = false,
            HasAccessToSystem = false
        });

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.WarehouseContactOutsideScope);
    }

    [Fact]
    public async Task CreateAsync_ForTheCallersOwnWarehouse_Succeeds()
    {
        var (organizationId, warehouseTokenA, warehouseIdA, _) = await SetupTwoWarehousesAsync("WHCONTACT_SCOPE_OWN");
        ActAsWarehouseContact(organizationId, warehouseIdA);

        var result = await Mediator.Send(new CreateWarehouseContactCommandRequest
        {
            WarehouseToken = warehouseTokenA,
            ContactName = "Should Be Allowed",
            IsPrimary = false,
            HasAccessToSystem = false
        });

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetPagedByWarehouseTokenAsync_ForAWarehouseOutsideTheCallersScope_ReturnsEmpty()
    {
        var (organizationId, warehouseTokenA, warehouseIdA, warehouseTokenB) = await SetupTwoWarehousesAsync("WHCONTACT_SCOPE_READ");

        // Create a real contact on Warehouse B first, as an Admin (already the org context after
        // SetupTwoWarehousesAsync) — proves data genuinely exists there for the read-scope check to hide.
        await Mediator.Send(new CreateWarehouseContactCommandRequest
        {
            WarehouseToken = warehouseTokenB,
            ContactName = "Belongs To B",
            IsPrimary = false,
            HasAccessToSystem = false
        });

        ActAsWarehouseContact(organizationId, warehouseIdA);

        var result = await Mediator.Send(new GetWarehouseContactsByWarehouseTokenQueryRequest
        {
            WarehouseToken = warehouseTokenB,
            PageNumber = 1,
            PageSize = 10
        });

        result.Success.Should().BeTrue();
        result.ReturnData!.WarehouseContacts.Should().BeEmpty();
    }
}
