using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Application.Responses.Common;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetEligibleReturnLinesQueryHandler(ISupplierReturnService supplierReturnService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetEligibleReturnLinesQueryRequest, ApiResponse<GetEligibleReturnLinesQueryResponse>>
    {
        public async Task<ApiResponse<GetEligibleReturnLinesQueryResponse>> Handle(GetEligibleReturnLinesQueryRequest request, CancellationToken cancellationToken)
        {
            var lines = await supplierReturnService.GetEligibleLinesAsync(request.PurchaseOrderToken, context, cancellationToken);

            if (lines is null)
                return ApiResponse<GetEligibleReturnLinesQueryResponse>.FailureResponse(ErrorCodes.PurchaseOrderNotFound, "Purchase order not found or access denied.", 404);

            return ApiResponse<GetEligibleReturnLinesQueryResponse>.SuccessResponse(new GetEligibleReturnLinesQueryResponse
            {
                Lines = mapper.MapList<EligibleReturnLine>(lines)
            });
        }
    }
}
