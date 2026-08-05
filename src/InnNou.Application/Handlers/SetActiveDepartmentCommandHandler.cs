using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetActiveDepartmentCommandHandler(IDepartmentService departmentService, IMapper mapper, IRequestContext context)
        : IRequestHandler<SetActiveDepartmentCommandRequest, ApiResponse<SetActiveDepartmentCommandResponse>>
    {
        public async Task<ApiResponse<SetActiveDepartmentCommandResponse>> Handle(SetActiveDepartmentCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.DepartmentToken == Guid.Empty)
                return ApiResponse<SetActiveDepartmentCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "DepartmentToken is required.", 400);

            var result = await departmentService.SetActiveAsync(request.DepartmentToken, request.IsActive, context, cancellationToken);
            if (result is null)
                return ApiResponse<SetActiveDepartmentCommandResponse>.FailureResponse(ErrorCodes.DepartmentNotFound, "Department not found.", 404);

            return ApiResponse<SetActiveDepartmentCommandResponse>.SuccessResponse(new SetActiveDepartmentCommandResponse
            {
                Department = mapper.Map<Responses.Common.Department>(result)
            });
        }
    }
}
