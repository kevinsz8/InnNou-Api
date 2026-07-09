using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSubCategoriesQueryHandler(ISubCategoryService subCategoryService, ICategoryService categoryService, IMapper mapper)
        : IRequestHandler<GetSubCategoriesQueryRequest, ApiResponse<GetSubCategoriesQueryResponse>>
    {
        public async Task<ApiResponse<GetSubCategoriesQueryResponse>> Handle(GetSubCategoriesQueryRequest request, CancellationToken cancellationToken)
        {
            int? categoryId = null;
            if (request.CategoryToken.HasValue)
            {
                var category = await categoryService.GetByTokenAsync(request.CategoryToken.Value, cancellationToken);
                if (category is null)
                    return ApiResponse<GetSubCategoriesQueryResponse>.FailureResponse(ErrorCodes.CategoryNotFound, "Category not found.", 404);
                categoryId = category.CategoryId;
            }

            var result = await subCategoryService.GetPagedAsync(request.PageNumber, request.PageSize, categoryId, request.SearchText, request.IncludeInactive, cancellationToken);
            var totalPages = result.TotalPages;
            var response = new GetSubCategoriesQueryResponse
            {
                SubCategories = mapper.MapList<Responses.Common.SubCategory>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.PageNumber < totalPages,
                HasPreviousPage = request.PageNumber > 1,
                NextPageNumber = request.PageNumber < totalPages ? request.PageNumber + 1 : (int?)null,
                PreviousPageNumber = request.PageNumber > 1 ? request.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetSubCategoriesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
