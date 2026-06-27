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
                    return ApiResponse<GetSubCategoriesQueryResponse>.FailureResponse("CATEGORY_NOT_FOUND", "Category not found.", 404);
                categoryId = category.CategoryId;
            }

            var items = await subCategoryService.GetAllAsync(categoryId, cancellationToken);
            var response = new GetSubCategoriesQueryResponse
            {
                SubCategories = mapper.MapList<Responses.Common.SubCategory>(items)
            };
            return ApiResponse<GetSubCategoriesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
