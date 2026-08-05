using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetDepartmentByTokenQueryHandler(IDepartmentService departmentService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetDepartmentByTokenQueryRequest, ApiResponse<GetDepartmentByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetDepartmentByTokenQueryResponse>> Handle(GetDepartmentByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            if (request.DepartmentToken == Guid.Empty)
                return ApiResponse<GetDepartmentByTokenQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "DepartmentToken is required.", 400);

            var result = await departmentService.GetByTokenAsync(request.DepartmentToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<GetDepartmentByTokenQueryResponse>.FailureResponse(ErrorCodes.DepartmentNotFound, "Department not found.", 404);

            return ApiResponse<GetDepartmentByTokenQueryResponse>.SuccessResponse(new GetDepartmentByTokenQueryResponse
            {
                Department = mapper.Map<Responses.Common.Department>(result)
            });
        }
    }
}
