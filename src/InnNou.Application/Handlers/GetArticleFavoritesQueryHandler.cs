using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetArticleFavoritesQueryHandler(IArticleFavoriteService articleFavoriteService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetArticleFavoritesQueryRequest, ApiResponse<GetArticleFavoritesQueryResponse>>
    {
        public async Task<ApiResponse<GetArticleFavoritesQueryResponse>> Handle(GetArticleFavoritesQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await articleFavoriteService.GetEffectiveAsync(
                request.PageNumber, request.PageSize, request.OrganizationToken, request.SearchText, request.IncludeInactive, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetArticleFavoritesQueryResponse
            {
                ArticleFavorites = mapper.MapList<Responses.Common.ArticleFavorite>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.PageNumber < totalPages,
                HasPreviousPage = request.PageNumber > 1,
                NextPageNumber = request.PageNumber < totalPages ? request.PageNumber + 1 : (int?)null,
                PreviousPageNumber = request.PageNumber > 1 ? request.PageNumber - 1 : (int?)null
            };

            return ApiResponse<GetArticleFavoritesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
