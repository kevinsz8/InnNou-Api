using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetActiveCategoryCommandHandler(ICategoryService categoryService, IMapper mapper)
        : IRequestHandler<SetActiveCategoryCommandRequest, ApiResponse<SetActiveCategoryCommandResponse>>
    {
        public async Task<ApiResponse<SetActiveCategoryCommandResponse>> Handle(SetActiveCategoryCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await categoryService.SetActiveAsync(request.CategoryToken, request.IsActive, cancellationToken);
            if (result is null)
                return ApiResponse<SetActiveCategoryCommandResponse>.FailureResponse("CATEGORY_NOT_FOUND", "Category not found.", 404);

            var response = new SetActiveCategoryCommandResponse { Category = mapper.Map<Responses.Common.Category>(result) };
            return ApiResponse<SetActiveCategoryCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
