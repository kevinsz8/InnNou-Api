using FluentAssertions;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Domain.Dtos;
using InnNou.IntegrationTests.TestFixtures;
using Xunit;

namespace InnNou.IntegrationTests.Orders;

/// <summary>
/// Regression coverage for the batch add-lines endpoint (2026-08-07 Performance Optimization
/// backlog item #4): AddOrderLinesCommandRequest validates the Order once (existence/
/// organization-scope/Draft-status) instead of once per line, then adds every requested line via
/// OrderService's shared AddLineToValidatedOrderAsync helper — best-effort, same convention as
/// ImportLinesAsync's Excel-row loop (one line's failure never aborts the rest).
/// </summary>
public class AddOrderLinesBatchTests(DatabaseFixture fixture) : TransactionalTestBase(fixture)
{
    private const decimal UnitPrice = 50.00m;

    private async Task<(Guid WarehouseToken, Guid SupplierToken, Guid FamilyToken)> SetupCatalogAsync(string namePrefix)
    {
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        var esJurisdiction = await Data.GetTaxJurisdictionTokenAsync("ES_MAINLAND_BALEARIC");
        var familyToken = await Data.CreateFamilyAsync($"{namePrefix}_FAM");

        Context.RoleLevel = 80;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";

        var warehouseToken = await Data.CreateWarehouseAsync(organizationToken, esJurisdiction, $"{namePrefix} Warehouse");
        var supplierToken = await Data.CreateSupplierAsync(organizationToken, $"{namePrefix} Supplier");

        return (warehouseToken, supplierToken, familyToken);
    }

    [Fact]
    public async Task AddLines_AllValid_AddsEveryLineAndReportsFullSuccess()
    {
        var (warehouseToken, supplierToken, familyToken) = await SetupCatalogAsync("ADDLINES_ALL_OK");
        var article1 = await Data.CreateArticleAsync(supplierToken, familyToken, "ADDLINES_ALL_OK Article 1");
        await Data.CreateArticlePriceAsync(article1, price: UnitPrice);
        var article2 = await Data.CreateArticleAsync(supplierToken, familyToken, "ADDLINES_ALL_OK Article 2");
        await Data.CreateArticlePriceAsync(article2, price: UnitPrice);

        var order = await Mediator.Send(new CreateOrderCommandRequest { WarehouseToken = warehouseToken });
        order.Success.Should().BeTrue(string.Join("; ", order.Errors.Select(e => $"{e.Code}: {e.Description}")));
        var orderToken = order.ReturnData!.Order!.OrderToken;

        var result = await Mediator.Send(new AddOrderLinesCommandRequest
        {
            OrderToken = orderToken,
            Lines =
            [
                new AddOrderLineInputDto { ArticleToken = article1, Quantity = 2 },
                new AddOrderLineInputDto { ArticleToken = article2, Quantity = 3 },
            ]
        });

        result.Success.Should().BeTrue(string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}")));
        result.ReturnData!.TotalLines.Should().Be(2);
        result.ReturnData!.SucceededCount.Should().Be(2);
        result.ReturnData!.FailureCount.Should().Be(0);
        result.ReturnData!.Errors.Should().BeEmpty();

        var fetched = await Data.GetOrderAsync(orderToken);
        fetched.Lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddLines_OneInvalidArticleAmongValidOnes_ReportsPartialSuccess()
    {
        var (warehouseToken, supplierToken, familyToken) = await SetupCatalogAsync("ADDLINES_PARTIAL");
        var article1 = await Data.CreateArticleAsync(supplierToken, familyToken, "ADDLINES_PARTIAL Article 1");
        await Data.CreateArticlePriceAsync(article1, price: UnitPrice);
        var article3 = await Data.CreateArticleAsync(supplierToken, familyToken, "ADDLINES_PARTIAL Article 3");
        await Data.CreateArticlePriceAsync(article3, price: UnitPrice);

        var order = await Mediator.Send(new CreateOrderCommandRequest { WarehouseToken = warehouseToken });
        var orderToken = order.ReturnData!.Order!.OrderToken;

        var bogusArticleToken = Guid.NewGuid();

        var result = await Mediator.Send(new AddOrderLinesCommandRequest
        {
            OrderToken = orderToken,
            Lines =
            [
                new AddOrderLineInputDto { ArticleToken = article1, Quantity = 1 },
                new AddOrderLineInputDto { ArticleToken = bogusArticleToken, Quantity = 1 },
                new AddOrderLineInputDto { ArticleToken = article3, Quantity = 1 },
            ]
        });

        result.Success.Should().BeTrue(string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}")));
        result.ReturnData!.TotalLines.Should().Be(3);
        result.ReturnData!.SucceededCount.Should().Be(2);
        result.ReturnData!.FailureCount.Should().Be(1);
        result.ReturnData!.Errors.Should().ContainSingle(e => e.Index == 1 && e.ArticleToken == bogusArticleToken && e.Code == ErrorCodes.ArticleNotFound);

        var fetched = await Data.GetOrderAsync(orderToken);
        fetched.Lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddLines_OrderNotFound_ReturnsCleanNotFound()
    {
        var result = await Mediator.Send(new AddOrderLinesCommandRequest
        {
            OrderToken = Guid.NewGuid(),
            Lines = [new AddOrderLineInputDto { ArticleToken = Guid.NewGuid(), Quantity = 1 }]
        });

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.OrderNotFound);
    }

    [Fact]
    public async Task AddLines_EmptyLinesList_IsRejected()
    {
        var (warehouseToken, _, _) = await SetupCatalogAsync("ADDLINES_EMPTY");
        var order = await Mediator.Send(new CreateOrderCommandRequest { WarehouseToken = warehouseToken });
        var orderToken = order.ReturnData!.Order!.OrderToken;

        var result = await Mediator.Send(new AddOrderLinesCommandRequest { OrderToken = orderToken, Lines = [] });

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.OrderAddLinesEmpty);
    }
}
