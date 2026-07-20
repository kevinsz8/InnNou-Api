using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateCategoryCommandHandler(ICategoryService categoryService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateCategoryCommandRequest, ApiResponse<CreateCategoryCommandResponse>>
    {
        public async Task<ApiResponse<CreateCategoryCommandResponse>> Handle(CreateCategoryCommandRequest request, CancellationToken cancellationToken)
        {
            var dto = new CategoryDto { Code = request.Code, OrganizationToken = request.OrganizationToken };
            var result = await categoryService.CreateAsync(dto, context, cancellationToken: cancellationToken);
            if (result is null)
                return ApiResponse<CreateCategoryCommandResponse>.FailureResponse(ErrorCodes.CategoryCreateFailed, "Category could not be created.", 500);

            var response = new CreateCategoryCommandResponse { Category = mapper.Map<Responses.Common.Category>(result) };
            return ApiResponse<CreateCategoryCommandResponse>.SuccessResponse(response, 201);
        }
    }
}
