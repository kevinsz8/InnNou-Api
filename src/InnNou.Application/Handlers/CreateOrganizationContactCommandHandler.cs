using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateOrganizationContactCommandHandler(IOrganizationContactService organizationContactService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateOrganizationContactCommandRequest, ApiResponse<CreateOrganizationContactCommandResponse>>
    {
        public async Task<ApiResponse<CreateOrganizationContactCommandResponse>> Handle(CreateOrganizationContactCommandRequest request, CancellationToken cancellationToken)
        {
            var dto = mapper.Map<OrganizationContactDto>(request);
            var result = await organizationContactService.CreateAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateOrganizationContactCommandResponse>.FailureResponse(ErrorCodes.OrganizationContactCreateFailed, "Failed to create organization contact.", 400);

            return ApiResponse<CreateOrganizationContactCommandResponse>.SuccessResponse(mapper.Map<CreateOrganizationContactCommandResponse>(result), 201);
        }
    }
}
