using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetActiveSubCategoryCommandHandler(ISubCategoryService subCategoryService, IMapper mapper, IRequestContext context)
        : IRequestHandler<SetActiveSubCategoryCommandRequest, ApiResponse<SetActiveSubCategoryCommandResponse>>
    {
        public async Task<ApiResponse<SetActiveSubCategoryCommandResponse>> Handle(SetActiveSubCategoryCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await subCategoryService.SetActiveAsync(request.SubCategoryToken, request.IsActive, context, cancellationToken);
            if (result is null)
                return ApiResponse<SetActiveSubCategoryCommandResponse>.FailureResponse(ErrorCodes.SubCategoryNotFound, "Sub-category not found.", 404);

            var response = new SetActiveSubCategoryCommandResponse { SubCategory = mapper.Map<Responses.Common.SubCategory>(result) };
            return ApiResponse<SetActiveSubCategoryCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
