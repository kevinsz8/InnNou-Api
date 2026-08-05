using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetRequisitionByTokenQueryHandler(IRequisitionService requisitionService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetRequisitionByTokenQueryRequest, ApiResponse<GetRequisitionByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetRequisitionByTokenQueryResponse>> Handle(GetRequisitionByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            if (request.RequisitionToken == Guid.Empty)
                return ApiResponse<GetRequisitionByTokenQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "RequisitionToken is required.", 400);

            var result = await requisitionService.GetByTokenAsync(request.RequisitionToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<GetRequisitionByTokenQueryResponse>.FailureResponse(ErrorCodes.RequisitionNotFound, "Requisition not found.", 404);

            return ApiResponse<GetRequisitionByTokenQueryResponse>.SuccessResponse(new GetRequisitionByTokenQueryResponse
            {
                Requisition = mapper.Map<Responses.Common.Requisition>(result)
            });
        }
    }
}
