using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateArticlePriceCommandHandler(IArticlePriceService articlePriceService, IArticleService articleService, IOrganizationService organizationService, ICurrencyService currencyService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateArticlePriceCommandRequest, ApiResponse<CreateArticlePriceCommandResponse>>
    {
        public async Task<ApiResponse<CreateArticlePriceCommandResponse>> Handle(CreateArticlePriceCommandRequest request, CancellationToken cancellationToken)
        {
            var article = await articleService.GetByTokenAsync(request.ArticleToken, context, cancellationToken);
            if (article is null)
                return ApiResponse<CreateArticlePriceCommandResponse>.FailureResponse(ErrorCodes.ArticleNotFound, "Article not found.", 404);

            if (article.ReplacedByArticleId.HasValue)
                return ApiResponse<CreateArticlePriceCommandResponse>.FailureResponse(
                    ErrorCodes.ArticlePriceArticleReplaced,
                    "This article has been superseded — price the replacement article instead.",
                    409);

            int? organizationId = null;
            if (request.OrganizationToken.HasValue)
            {
                var organization = await organizationService.GetOrganizationByTokenAsync(request.OrganizationToken.Value, context, cancellationToken);
                if (organization is null)
                    return ApiResponse<CreateArticlePriceCommandResponse>.FailureResponse(ErrorCodes.OrganizationNotFound, "Organization not found.", 404);
                organizationId = organization.OrganizationId;
            }

            var currencyCode = (request.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
            if (!await currencyService.ExistsActiveByCodeAsync(currencyCode, cancellationToken))
                return ApiResponse<CreateArticlePriceCommandResponse>.FailureResponse(
                    ErrorCodes.ArticlePriceInvalidCurrency,
                    "Currency code must be a recognized, active ISO 4217 code (e.g. EUR, USD).",
                    400);

            if (request.Price <= 0)
                return ApiResponse<CreateArticlePriceCommandResponse>.FailureResponse(ErrorCodes.ArticlePriceInvalidAmount, "Price must be greater than zero.", 400);

            var dto = new ArticlePriceDto
            {
                ArticleId = article.ArticleId,
                SupplierId = article.SupplierId,
                OrganizationId = organizationId,
                Price = request.Price,
                CurrencyCode = currencyCode,
                EffectiveDate = request.EffectiveDate ?? DateTime.UtcNow.Date,
                Notes = request.Notes
            };

            var result = await articlePriceService.CreateAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateArticlePriceCommandResponse>.FailureResponse(ErrorCodes.ArticlePriceCreateFailed, "Article price could not be created.", 500);

            return ApiResponse<CreateArticlePriceCommandResponse>.SuccessResponse(
                new CreateArticlePriceCommandResponse { ArticlePrice = mapper.Map<Responses.Common.ArticlePrice>(result) }, 201);
        }
    }
}
