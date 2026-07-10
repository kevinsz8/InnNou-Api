using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using Microsoft.Data.SqlClient;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class ArticlePriceService(IDbConnectionFactory connectionFactory, IMapper mapper) : IArticlePriceService
{
    private sealed class ArticlePricePageRow : ArticlePrice { public int TotalCount { get; set; } }

    private const int AdminRoleLevel = 80;
    private const int MaxPageSize = 100;

    // Harmonized with ArticleService.CanManage: suppliers own ArticlePrices, so only the owning
    // supplier (real login or impersonated) or Admin+ may write them.
    private static bool CanManage(IRequestContext context, int supplierId) =>
        context.SupplierId.HasValue
            ? context.SupplierId.Value == supplierId
            : context.RoleLevel >= AdminRoleLevel;

    public async Task<ArticlePriceDto?> CreateAsync(ArticlePriceDto dto, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!CanManage(context, dto.SupplierId))
            throw new ApiException(ErrorCodes.ArticlePriceSupplierForbidden, "Not allowed to set prices for this supplier's articles.", 403);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ArticlePriceToken", Guid.NewGuid());
        p.Add("@ArticleId", dto.ArticleId);
        p.Add("@OrganizationId", dto.OrganizationId);
        p.Add("@Price", dto.Price);
        p.Add("@CurrencyCode", dto.CurrencyCode);
        p.Add("@EffectiveDate", dto.EffectiveDate);
        p.Add("@Notes", dto.Notes);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());

        try
        {
            var row = await connection.QueryFirstOrDefaultAsync<ArticlePrice>(
                "sp_ArticlePrice_Create", p, commandType: CommandType.StoredProcedure);
            return row is null ? null : mapper.Map<ArticlePriceDto>(row);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new ApiException(
                ErrorCodes.ArticlePriceDuplicateEffectiveDate,
                "A price for this article/organization/currency is already effective on this date.",
                409);
        }
    }

    public async Task<ArticlePriceDto?> GetCurrentAsync(int articleId, int supplierId, int? requestedOrganizationId, string? currencyCode, DateTime asOfDate, IRequestContext context, CancellationToken cancellationToken = default)
    {
        // Everyone may read a current price; a regular org-scoped caller always resolves it for
        // their own (server-known) organization. Only the owning supplier or Admin+ may inspect
        // an explicitly requested organization's contract price.
        var effectiveOrganizationId = CanManage(context, supplierId) ? requestedOrganizationId : context.OrganizationId;

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ArticleId", articleId);
        p.Add("@OrganizationId", effectiveOrganizationId);
        p.Add("@CurrencyCode", currencyCode, DbType.AnsiString, size: 10, direction: ParameterDirection.InputOutput);
        p.Add("@AsOfDate", asOfDate);

        var row = await connection.QueryFirstOrDefaultAsync<ArticlePrice>(
            "sp_ArticlePrice_GetCurrent", p, commandType: CommandType.StoredProcedure);

        var resolvedCurrencyCode = p.Get<string?>("@CurrencyCode");
        if (resolvedCurrencyCode is null)
            throw new ApiException(
                ErrorCodes.ArticlePriceCurrencyRequired,
                "A currency code could not be determined — supply one explicitly or resolve for an organization with a configured CurrencyCode.",
                400);

        return row is null ? null : mapper.Map<ArticlePriceDto>(row);
    }

    public async Task<PagedResult<ArticlePriceDto>> GetHistoryAsync(int pageNumber, int pageSize, int articleId, int supplierId, int? requestedOrganizationId, string? currencyCode, IRequestContext context, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        // Owning supplier/Admin see full history across every organization's contract prices;
        // everyone else is forced to their own (server-known) organization plus global prices.
        var unrestricted = CanManage(context, supplierId);
        var effectiveOrganizationId = unrestricted ? requestedOrganizationId : context.OrganizationId;

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@ArticleId", articleId);
        p.Add("@OrganizationId", effectiveOrganizationId);
        p.Add("@CurrencyCode", currencyCode);
        p.Add("@UnrestrictedOrganizationAccess", unrestricted);
        var rows = (await connection.QueryAsync<ArticlePricePageRow>(
            "sp_ArticlePrice_GetHistory", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<ArticlePriceDto>
        {
            Items = mapper.MapList<ArticlePriceDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }
}
