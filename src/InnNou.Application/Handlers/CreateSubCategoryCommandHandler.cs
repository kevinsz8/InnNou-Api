using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateSubCategoryCommandHandler(ISubCategoryService subCategoryService, ICategoryService categoryService, IMapper mapper)
        : IRequestHandler<CreateSubCategoryCommandRequest, ApiResponse<CreateSubCategoryCommandResponse>>
    {
        public async Task<ApiResponse<CreateSubCategoryCommandResponse>> Handle(CreateSubCategoryCommandRequest request, CancellationToken cancellationToken)
        {
            var category = await categoryService.GetByTokenAsync(request.CategoryToken, cancellationToken);
            if (category is null)
                return ApiResponse<CreateSubCategoryCommandResponse>.FailureResponse(ErrorCodes.CategoryNotFound, "Category not found.", 404);

            if (await subCategoryService.ExistsByCodeAsync(request.Code, category.CategoryId, cancellationToken))
                return ApiResponse<CreateSubCategoryCommandResponse>.FailureResponse(ErrorCodes.SubCategoryCodeExists, "A sub-category with this code already exists in the category.", 409);

            var dto = new SubCategoryDto { CategoryId = category.CategoryId, Code = request.Code };
            var result = await subCategoryService.CreateAsync(dto, cancellationToken);
            if (result is null)
                return ApiResponse<CreateSubCategoryCommandResponse>.FailureResponse(ErrorCodes.SubCategoryCreateFailed, "Sub-category could not be created.", 500);

            var response = new CreateSubCategoryCommandResponse { SubCategory = mapper.Map<Responses.Common.SubCategory>(result) };
            return ApiResponse<CreateSubCategoryCommandResponse>.SuccessResponse(response, 201);
        }
    }
}
