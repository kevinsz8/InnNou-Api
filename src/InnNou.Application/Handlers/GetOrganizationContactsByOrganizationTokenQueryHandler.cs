using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetOrganizationContactsByOrganizationTokenQueryHandler(IOrganizationContactService organizationContactService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetOrganizationContactsByOrganizationTokenQueryRequest, ApiResponse<GetOrganizationContactsByOrganizationTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetOrganizationContactsByOrganizationTokenQueryResponse>> Handle(GetOrganizationContactsByOrganizationTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await organizationContactService.GetPagedByOrganizationTokenAsync(
                request.OrganizationToken, request.PageNumber, request.PageSize,
                request.SearchText, request.IncludeInactive, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetOrganizationContactsByOrganizationTokenQueryResponse
            {
                OrganizationContacts = mapper.MapList<Responses.Common.OrganizationContact>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetOrganizationContactsByOrganizationTokenQueryResponse>.SuccessResponse(response);
        }
    }
}
