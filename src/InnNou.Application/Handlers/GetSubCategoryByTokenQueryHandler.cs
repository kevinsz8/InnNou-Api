using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSubCategoryByTokenQueryHandler(ISubCategoryService subCategoryService, IMapper mapper)
        : IRequestHandler<GetSubCategoryByTokenQueryRequest, ApiResponse<GetSubCategoryByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetSubCategoryByTokenQueryResponse>> Handle(GetSubCategoryByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var dto = await subCategoryService.GetByTokenAsync(request.SubCategoryToken, cancellationToken);
            if (dto is null)
                return ApiResponse<GetSubCategoryByTokenQueryResponse>.FailureResponse(ErrorCodes.SubCategoryNotFound, "Sub-category not found.", 404);

            var response = new GetSubCategoryByTokenQueryResponse { SubCategory = mapper.Map<Responses.Common.SubCategory>(dto) };
            return ApiResponse<GetSubCategoryByTokenQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
