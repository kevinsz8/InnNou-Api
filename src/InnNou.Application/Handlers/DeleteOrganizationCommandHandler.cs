using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteOrganizationCommandHandler : IRequestHandler<DeleteOrganizationCommandRequest, ApiResponse<DeleteOrganizationCommandResponse>>
    {
        private readonly IOrganizationService _organizationService;
        private readonly IRequestContext _context;

        public DeleteOrganizationCommandHandler(IOrganizationService organizationService, IRequestContext context)
        {
            _organizationService = organizationService;
            _context = context;
        }

        public async Task<ApiResponse<DeleteOrganizationCommandResponse>> Handle(DeleteOrganizationCommandRequest request, CancellationToken cancellationToken)
        {
            var success = await _organizationService.DeleteOrganizationAsync(request.OrganizationToken, _context, cancellationToken);

            if (!success)
                return ApiResponse<DeleteOrganizationCommandResponse>.FailureResponse("ORGANIZATION_DELETE_FAILED", "Organization could not be deleted.");

            return ApiResponse<DeleteOrganizationCommandResponse>.SuccessResponse(
                new DeleteOrganizationCommandResponse { OrganizationToken = request.OrganizationToken, Success = true });
        }
    }
}
