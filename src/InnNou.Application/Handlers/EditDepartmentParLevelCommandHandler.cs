using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditDepartmentParLevelCommandHandler(IDepartmentParLevelService departmentParLevelService, IMapper mapper, IRequestContext context)
        : IRequestHandler<EditDepartmentParLevelCommandRequest, ApiResponse<EditDepartmentParLevelCommandResponse>>
    {
        public async Task<ApiResponse<EditDepartmentParLevelCommandResponse>> Handle(EditDepartmentParLevelCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.DepartmentParLevelToken == Guid.Empty)
                return ApiResponse<EditDepartmentParLevelCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "DepartmentParLevelToken is required.", 400);

            if (request.MinimumQuantity < 0)
                return ApiResponse<EditDepartmentParLevelCommandResponse>.FailureResponse(ErrorCodes.DepartmentParLevelInvalidQuantity, "Minimum quantity cannot be negative.", 400);

            if (request.ReorderQuantity <= 0)
                return ApiResponse<EditDepartmentParLevelCommandResponse>.FailureResponse(ErrorCodes.DepartmentParLevelInvalidQuantity, "Reorder quantity must be greater than zero.", 400);

            var result = await departmentParLevelService.EditAsync(request.DepartmentParLevelToken, request.MinimumQuantity, request.ReorderQuantity, context, cancellationToken);
            if (result is null)
                return ApiResponse<EditDepartmentParLevelCommandResponse>.FailureResponse(ErrorCodes.DepartmentParLevelNotFound, "Department par level not found.", 404);

            return ApiResponse<EditDepartmentParLevelCommandResponse>.SuccessResponse(new EditDepartmentParLevelCommandResponse
            {
                DepartmentParLevel = mapper.Map<Responses.Common.DepartmentParLevel>(result)
            });
        }
    }
}
