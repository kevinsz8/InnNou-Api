using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateDepartmentCommandHandler(IDepartmentService departmentService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateDepartmentCommandRequest, ApiResponse<CreateDepartmentCommandResponse>>
    {
        public async Task<ApiResponse<CreateDepartmentCommandResponse>> Handle(CreateDepartmentCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrganizationToken == Guid.Empty)
                return ApiResponse<CreateDepartmentCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrganizationToken is required.", 400);

            if (string.IsNullOrWhiteSpace(request.Name))
                return ApiResponse<CreateDepartmentCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Name is required.", 400);

            var dto = mapper.Map<DepartmentDto>(request);

            var result = await departmentService.CreateAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateDepartmentCommandResponse>.FailureResponse(ErrorCodes.DepartmentOrganizationNotFound, "Organization not found.", 404);

            return ApiResponse<CreateDepartmentCommandResponse>.SuccessResponse(new CreateDepartmentCommandResponse
            {
                Department = mapper.Map<Responses.Common.Department>(result)
            }, 201);
        }
    }
}
