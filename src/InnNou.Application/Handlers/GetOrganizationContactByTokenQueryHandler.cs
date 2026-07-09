using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetOrganizationContactByTokenQueryHandler(IOrganizationContactService organizationContactService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetOrganizationContactByTokenQueryRequest, ApiResponse<GetOrganizationContactByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetOrganizationContactByTokenQueryResponse>> Handle(GetOrganizationContactByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var contact = await organizationContactService.GetByTokenAsync(request.OrganizationContactToken, context, cancellationToken);
            if (contact is null)
                return ApiResponse<GetOrganizationContactByTokenQueryResponse>.FailureResponse(ErrorCodes.OrganizationContactNotFound, "Organization contact not found.", 404);

            return ApiResponse<GetOrganizationContactByTokenQueryResponse>.SuccessResponse(new GetOrganizationContactByTokenQueryResponse
            {
                OrganizationContact = mapper.Map<Responses.Common.OrganizationContact>(contact)
            });
        }
    }
}
