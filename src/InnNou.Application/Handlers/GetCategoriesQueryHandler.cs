using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetCategoriesQueryHandler(ICategoryService categoryService, IMapper mapper)
        : IRequestHandler<GetCategoriesQueryRequest, ApiResponse<GetCategoriesQueryResponse>>
    {
        public async Task<ApiResponse<GetCategoriesQueryResponse>> Handle(GetCategoriesQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await categoryService.GetPagedAsync(request.PageNumber, request.PageSize, request.SearchText, cancellationToken);
            var totalPages = result.TotalPages;
            var response = new GetCategoriesQueryResponse
            {
                Categories = mapper.MapList<Responses.Common.Category>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.PageNumber < totalPages,
                HasPreviousPage = request.PageNumber > 1,
                NextPageNumber = request.PageNumber < totalPages ? request.PageNumber + 1 : (int?)null,
                PreviousPageNumber = request.PageNumber > 1 ? request.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetCategoriesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
