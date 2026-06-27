using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateCategoryCommandHandler(ICategoryService categoryService, IMapper mapper)
        : IRequestHandler<CreateCategoryCommandRequest, ApiResponse<CreateCategoryCommandResponse>>
    {
        public async Task<ApiResponse<CreateCategoryCommandResponse>> Handle(CreateCategoryCommandRequest request, CancellationToken cancellationToken)
        {
            if (await categoryService.ExistsByCodeAsync(request.Code, cancellationToken))
                return ApiResponse<CreateCategoryCommandResponse>.FailureResponse("CATEGORY_CODE_EXISTS", "A category with this code already exists.", 409);

            var dto = new CategoryDto { Code = request.Code };
            var result = await categoryService.CreateAsync(dto, cancellationToken);
            if (result is null)
                return ApiResponse<CreateCategoryCommandResponse>.FailureResponse("CATEGORY_CREATE_FAILED", "Category could not be created.", 500);

            var response = new CreateCategoryCommandResponse { Category = mapper.Map<Responses.Common.Category>(result) };
            return ApiResponse<CreateCategoryCommandResponse>.SuccessResponse(response, 201);
        }
    }
}
