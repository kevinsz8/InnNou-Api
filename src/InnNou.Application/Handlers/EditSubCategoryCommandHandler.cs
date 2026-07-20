using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditSubCategoryCommandHandler(ISubCategoryService subCategoryService, IMapper mapper, IRequestContext context)
        : IRequestHandler<EditSubCategoryCommandRequest, ApiResponse<EditSubCategoryCommandResponse>>
    {
        public async Task<ApiResponse<EditSubCategoryCommandResponse>> Handle(EditSubCategoryCommandRequest request, CancellationToken cancellationToken)
        {
            var dto = new SubCategoryDto { SubCategoryToken = request.SubCategoryToken, Code = request.Code };
            var result = await subCategoryService.EditAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<EditSubCategoryCommandResponse>.FailureResponse(ErrorCodes.SubCategoryNotFound, "Sub-category not found.", 404);

            var response = new EditSubCategoryCommandResponse { SubCategory = mapper.Map<Responses.Common.SubCategory>(result) };
            return ApiResponse<EditSubCategoryCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
