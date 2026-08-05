using InnNou.Application.Common;

namespace InnNou.IntegrationTests.TestFixtures;

/// <summary>
/// Settable <see cref="IRequestContext"/> for tests. Production's own <c>RequestContext</c>
/// reads JWT claims off <c>HttpContext</c>, which doesn't exist outside a real request - this
/// is registered over it (last registration wins for <c>GetService&lt;IRequestContext&gt;</c>)
/// so a test can just set the fields it needs, e.g. impersonate a SuperAdmin or a specific org.
/// </summary>
public class TestRequestContext : IRequestContext
{
    public Guid ActorUserToken { get; set; } = Guid.NewGuid();
    public Guid EffectiveUserToken { get; set; }
    public int? OrganizationId { get; set; }
    public string? OrganizationTypeCode { get; set; }
    public int? SupplierId { get; set; }
    public int? WarehouseId { get; set; }
    public int RoleLevel { get; set; } = 100;
    public int ActorRoleLevel { get; set; } = 100;
    public int? ActorOrganizationId { get; set; }
    public bool IsAuthenticated { get; set; } = true;
    public bool IsImpersonating => ActorUserToken != EffectiveUserToken;

    public TestRequestContext()
    {
        EffectiveUserToken = ActorUserToken;
    }

    /// <summary>SuperAdmin, no organization - matches a bare SuperAdmin session (RoleLevel 100).</summary>
    public static TestRequestContext SuperAdmin() => new() { RoleLevel = 100, ActorRoleLevel = 100 };

    /// <summary>An Admin (RoleLevel 80) scoped to a specific ASSOCIATE organization - the shape
    /// most write operations in this codebase actually authorize against.</summary>
    public static TestRequestContext AssociateAdmin(int organizationId) => new()
    {
        RoleLevel = 80,
        ActorRoleLevel = 80,
        OrganizationId = organizationId,
        ActorOrganizationId = organizationId,
        OrganizationTypeCode = "ASSOCIATE"
    };
}
