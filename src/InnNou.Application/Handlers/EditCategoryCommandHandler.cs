using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditCategoryCommandHandler(ICategoryService categoryService, IMapper mapper)
        : IRequestHandler<EditCategoryCommandRequest, ApiResponse<EditCategoryCommandResponse>>
    {
        public async Task<ApiResponse<EditCategoryCommandResponse>> Handle(EditCategoryCommandRequest request, CancellationToken cancellationToken)
        {
            var dto = new CategoryDto { CategoryToken = request.CategoryToken, Code = request.Code };
            var result = await categoryService.EditAsync(dto, cancellationToken);
            if (result is null)
                return ApiResponse<EditCategoryCommandResponse>.FailureResponse(ErrorCodes.CategoryNotFound, "Category not found.", 404);

            var response = new EditCategoryCommandResponse { Category = mapper.Map<Responses.Common.Category>(result) };
            return ApiResponse<EditCategoryCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
