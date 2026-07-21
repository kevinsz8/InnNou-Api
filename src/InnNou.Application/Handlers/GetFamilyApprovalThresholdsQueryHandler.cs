using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetFamilyApprovalThresholdsQueryHandler(IFamilyApprovalThresholdService service, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetFamilyApprovalThresholdsQueryRequest, ApiResponse<GetFamilyApprovalThresholdsQueryResponse>>
    {
        public async Task<ApiResponse<GetFamilyApprovalThresholdsQueryResponse>> Handle(GetFamilyApprovalThresholdsQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await service.GetPagedAsync(request.OrganizationToken, request.PageNumber, request.PageSize, request.FamilyToken, request.IncludeInactive, context, cancellationToken);
            var totalPages = result.TotalPages;
            var response = new GetFamilyApprovalThresholdsQueryResponse
            {
                FamilyApprovalThresholds = mapper.MapList<Responses.Common.FamilyApprovalThreshold>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetFamilyApprovalThresholdsQueryResponse>.SuccessResponse(response);
        }
    }
}
