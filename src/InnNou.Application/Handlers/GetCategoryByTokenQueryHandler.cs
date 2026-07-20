using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetCategoryByTokenQueryHandler(ICategoryService categoryService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetCategoryByTokenQueryRequest, ApiResponse<GetCategoryByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetCategoryByTokenQueryResponse>> Handle(GetCategoryByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var dto = await categoryService.GetByTokenAsync(request.CategoryToken, context, cancellationToken);
            if (dto is null)
                return ApiResponse<GetCategoryByTokenQueryResponse>.FailureResponse(ErrorCodes.CategoryNotFound, "Category not found.", 404);

            var response = new GetCategoryByTokenQueryResponse { Category = mapper.Map<Responses.Common.Category>(dto) };
            return ApiResponse<GetCategoryByTokenQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
