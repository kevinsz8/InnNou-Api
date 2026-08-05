using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateDepartmentParLevelCommandHandler(IDepartmentParLevelService departmentParLevelService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateDepartmentParLevelCommandRequest, ApiResponse<CreateDepartmentParLevelCommandResponse>>
    {
        public async Task<ApiResponse<CreateDepartmentParLevelCommandResponse>> Handle(CreateDepartmentParLevelCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.DepartmentToken == Guid.Empty)
                return ApiResponse<CreateDepartmentParLevelCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "DepartmentToken is required.", 400);

            if (request.ArticleToken == Guid.Empty)
                return ApiResponse<CreateDepartmentParLevelCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "ArticleToken is required.", 400);

            if (request.MinimumQuantity < 0)
                return ApiResponse<CreateDepartmentParLevelCommandResponse>.FailureResponse(ErrorCodes.DepartmentParLevelInvalidQuantity, "Minimum quantity cannot be negative.", 400);

            if (request.ReorderQuantity <= 0)
                return ApiResponse<CreateDepartmentParLevelCommandResponse>.FailureResponse(ErrorCodes.DepartmentParLevelInvalidQuantity, "Reorder quantity must be greater than zero.", 400);

            var result = await departmentParLevelService.CreateAsync(request.DepartmentToken, request.ArticleToken, request.MinimumQuantity, request.ReorderQuantity, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateDepartmentParLevelCommandResponse>.FailureResponse(ErrorCodes.DepartmentParLevelNotFound, "Department par level could not be created.", 404);

            return ApiResponse<CreateDepartmentParLevelCommandResponse>.SuccessResponse(new CreateDepartmentParLevelCommandResponse
            {
                DepartmentParLevel = mapper.Map<Responses.Common.DepartmentParLevel>(result)
            }, 201);
        }
    }
}
