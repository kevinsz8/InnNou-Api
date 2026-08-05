using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditDepartmentCommandHandler(IDepartmentService departmentService, IMapper mapper, IRequestContext context)
        : IRequestHandler<EditDepartmentCommandRequest, ApiResponse<EditDepartmentCommandResponse>>
    {
        public async Task<ApiResponse<EditDepartmentCommandResponse>> Handle(EditDepartmentCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.DepartmentToken == Guid.Empty)
                return ApiResponse<EditDepartmentCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "DepartmentToken is required.", 400);

            if (string.IsNullOrWhiteSpace(request.Name))
                return ApiResponse<EditDepartmentCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Name is required.", 400);

            var dto = mapper.Map<DepartmentDto>(request);

            var result = await departmentService.EditAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<EditDepartmentCommandResponse>.FailureResponse(ErrorCodes.DepartmentNotFound, "Department not found.", 404);

            return ApiResponse<EditDepartmentCommandResponse>.SuccessResponse(new EditDepartmentCommandResponse
            {
                Department = mapper.Map<Responses.Common.Department>(result)
            });
        }
    }
}
