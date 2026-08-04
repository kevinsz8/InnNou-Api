using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetInternalOrderEligibleSourceOrganizationsQueryHandler(IInternalOrderService internalOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetInternalOrderEligibleSourceOrganizationsQueryRequest, ApiResponse<GetInternalOrderEligibleSourceOrganizationsQueryResponse>>
    {
        public async Task<ApiResponse<GetInternalOrderEligibleSourceOrganizationsQueryResponse>> Handle(GetInternalOrderEligibleSourceOrganizationsQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await internalOrderService.GetEligibleSourceOrganizationsAsync(context, cancellationToken);

            return ApiResponse<GetInternalOrderEligibleSourceOrganizationsQueryResponse>.SuccessResponse(new GetInternalOrderEligibleSourceOrganizationsQueryResponse
            {
                Organizations = mapper.MapList<Responses.Common.Organization>(result)
            });
        }
    }
}
