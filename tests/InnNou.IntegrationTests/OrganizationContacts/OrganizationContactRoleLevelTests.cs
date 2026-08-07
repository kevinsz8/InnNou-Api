using FluentAssertions;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.IntegrationTests.TestFixtures;
using Xunit;

namespace InnNou.IntegrationTests.OrganizationContacts;

/// <summary>
/// Regression coverage for a gap found in the 2026-08-07 full-system audit:
/// OrganizationContactService.CanManageOrganizationAsync had no RoleLevel floor on its
/// org-hierarchy branch — any authenticated caller belonging to the organization's own hierarchy,
/// even a "Regular user" (RoleLevel 0/1 per CLAUDE.md's role table), could Create/Edit/Delete that
/// organization's contacts. Every sibling contact service (WarehouseContactService) already
/// required RoleLevel &gt;= Staff (20) for writes. Fixed by splitting the single shared check into
/// CanManageOrganizationAsync (writes, now Staff-gated) and CanManageReadAsync (reads, unchanged —
/// a regular org member looking up a contact was never the reported problem).
/// </summary>
public class OrganizationContactRoleLevelTests(DatabaseFixture fixture) : TransactionalTestBase(fixture)
{
    private const int RegularUserRoleLevel = 1;
    private const int StaffRoleLevel = 20;

    private void ActAsRegularUser(int organizationId)
    {
        Context.RoleLevel = RegularUserRoleLevel;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";
    }

    private void ActAsStaff(int organizationId)
    {
        Context.RoleLevel = StaffRoleLevel;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";
    }

    [Fact]
    public async Task CreateAsync_AsARegularUserBelowStaffRoleLevel_IsForbidden()
    {
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        ActAsRegularUser(organizationId);

        var result = await Mediator.Send(new CreateOrganizationContactCommandRequest
        {
            OrganizationToken = organizationToken,
            ContactName = "Should Be Blocked",
            IsPrimary = false
        });

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.OrganizationContactOutsideScope);
    }

    [Fact]
    public async Task CreateAsync_AsStaffRoleLevel_Succeeds()
    {
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        ActAsStaff(organizationId);

        var result = await Mediator.Send(new CreateOrganizationContactCommandRequest
        {
            OrganizationToken = organizationToken,
            ContactName = "Should Be Allowed",
            IsPrimary = false
        });

        result.Success.Should().BeTrue(string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}")));
    }

    [Fact]
    public async Task GetPaged_AsARegularUserBelowStaffRoleLevel_StillSucceeds()
    {
        // Reads were never the reported problem — confirms the fix didn't over-correct and lock
        // a regular org member out of viewing their own organization's contacts.
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        ActAsRegularUser(organizationId);

        var result = await Mediator.Send(new GetOrganizationContactsByOrganizationTokenQueryRequest
        {
            OrganizationToken = organizationToken,
            PageNumber = 1,
            PageSize = 10
        });

        result.Success.Should().BeTrue();
    }
}
