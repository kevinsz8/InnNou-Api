using FluentAssertions;
using InnNou.IntegrationTests.TestFixtures;
using Xunit;

namespace InnNou.IntegrationTests.Articles;

/// <summary>
/// Regression coverage for the 2026-08-07 fix: superseding an article with an explicit
/// TaxCategoryToken override used to be silently dropped by the frontend (a stale generated TS
/// type + a submit branch that simply never sent the field), so the backend's own correct
/// "request.TaxCategoryToken ?? existing.TaxCategoryToken" fallback always took the existing
/// branch. That bug lived entirely in InnNou-Web and can't be caught here - but the backend
/// contract it depends on (an explicit override taking precedence over the original) had no test
/// at all before this. Exercised through the real Supersede command, never a raw SQL check.
/// </summary>
public class ArticleSupersedeTests(DatabaseFixture fixture) : TransactionalTestBase(fixture)
{
    [Fact]
    public async Task Supersede_WithAnExplicitTaxCategoryOverride_AppliesTheOverrideNotTheOriginal()
    {
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        Context.RoleLevel = 80;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";

        var familyToken = await Data.CreateFamilyAsync("SUPERSEDE_TAX");
        var supplierToken = await Data.CreateSupplierAsync(organizationToken, "SUPERSEDE_TAX");

        var generalToken = await Data.GetTaxCategoryTokenAsync("GENERAL");
        var reducedToken = await Data.GetTaxCategoryTokenAsync("REDUCED");
        var articleToken = await Data.CreateArticleAsync(supplierToken, familyToken, "SUPERSEDE_TAX", taxCategoryToken: generalToken);

        var superseded = await Data.SupersedeArticleAsync(articleToken, "SUPERSEDE_TAX Replacement", taxCategoryToken: reducedToken);

        superseded.TaxCategoryCode.Should().Be("REDUCED",
            "an explicit override on Supersede must take precedence over the original article's own tax category, not silently fall back to it");
    }

    [Fact]
    public async Task Supersede_WithNoTaxCategoryOverride_CarriesTheOriginalForward()
    {
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        Context.RoleLevel = 80;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";

        var familyToken = await Data.CreateFamilyAsync("SUPERSEDE_INHERIT");
        var supplierToken = await Data.CreateSupplierAsync(organizationToken, "SUPERSEDE_INHERIT");

        var generalToken = await Data.GetTaxCategoryTokenAsync("GENERAL");
        var articleToken = await Data.CreateArticleAsync(supplierToken, familyToken, "SUPERSEDE_INHERIT", taxCategoryToken: generalToken);

        var superseded = await Data.SupersedeArticleAsync(articleToken, "SUPERSEDE_INHERIT Replacement", taxCategoryToken: null);

        superseded.TaxCategoryCode.Should().Be("GENERAL",
            "omitting the override must carry the original article's own tax category forward, not reset it to the Family's default");
    }
}
