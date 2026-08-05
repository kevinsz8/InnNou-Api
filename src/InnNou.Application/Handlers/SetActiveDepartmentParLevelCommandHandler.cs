using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetActiveDepartmentParLevelCommandHandler(IDepartmentParLevelService departmentParLevelService, IMapper mapper, IRequestContext context)
        : IRequestHandler<SetActiveDepartmentParLevelCommandRequest, ApiResponse<SetActiveDepartmentParLevelCommandResponse>>
    {
        public async Task<ApiResponse<SetActiveDepartmentParLevelCommandResponse>> Handle(SetActiveDepartmentParLevelCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.DepartmentParLevelToken == Guid.Empty)
                return ApiResponse<SetActiveDepartmentParLevelCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "DepartmentParLevelToken is required.", 400);

            var result = await departmentParLevelService.SetActiveAsync(request.DepartmentParLevelToken, request.IsActive, context, cancellationToken);
            if (result is null)
                return ApiResponse<SetActiveDepartmentParLevelCommandResponse>.FailureResponse(ErrorCodes.DepartmentParLevelNotFound, "Department par level not found.", 404);

            return ApiResponse<SetActiveDepartmentParLevelCommandResponse>.SuccessResponse(new SetActiveDepartmentParLevelCommandResponse
            {
                DepartmentParLevel = mapper.Map<Responses.Common.DepartmentParLevel>(result)
            });
        }
    }
}
