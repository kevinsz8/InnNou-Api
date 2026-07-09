using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteOrganizationContactCommandHandler(IOrganizationContactService organizationContactService, IRequestContext context)
        : IRequestHandler<DeleteOrganizationContactCommandRequest, ApiResponse<DeleteOrganizationContactCommandResponse>>
    {
        public async Task<ApiResponse<DeleteOrganizationContactCommandResponse>> Handle(DeleteOrganizationContactCommandRequest request, CancellationToken cancellationToken)
        {
            var deleted = await organizationContactService.DeleteAsync(request.OrganizationContactToken, context, cancellationToken);
            if (!deleted)
                return ApiResponse<DeleteOrganizationContactCommandResponse>.FailureResponse(ErrorCodes.OrganizationContactNotFound, "Organization contact not found.", 404);

            return ApiResponse<DeleteOrganizationContactCommandResponse>.SuccessResponse(new DeleteOrganizationContactCommandResponse
            {
                OrganizationContactToken = request.OrganizationContactToken,
                Success = true
            });
        }
    }
}
