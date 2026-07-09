using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditOrganizationContactCommandHandler(IOrganizationContactService organizationContactService, IMapper mapper, IRequestContext context)
        : IRequestHandler<EditOrganizationContactCommandRequest, ApiResponse<EditOrganizationContactCommandResponse>>
    {
        public async Task<ApiResponse<EditOrganizationContactCommandResponse>> Handle(EditOrganizationContactCommandRequest request, CancellationToken cancellationToken)
        {
            var dto = mapper.Map<OrganizationContactDto>(request);
            var result = await organizationContactService.EditAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<EditOrganizationContactCommandResponse>.FailureResponse(ErrorCodes.OrganizationContactNotFound, "Organization contact not found.", 404);

            return ApiResponse<EditOrganizationContactCommandResponse>.SuccessResponse(mapper.Map<EditOrganizationContactCommandResponse>(result));
        }
    }
}
