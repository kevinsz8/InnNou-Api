using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetInternalOrderByTokenQueryHandler(IInternalOrderService internalOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetInternalOrderByTokenQueryRequest, ApiResponse<GetInternalOrderByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetInternalOrderByTokenQueryResponse>> Handle(GetInternalOrderByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            if (request.InternalOrderToken == Guid.Empty)
                return ApiResponse<GetInternalOrderByTokenQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "InternalOrderToken is required.", 400);

            var result = await internalOrderService.GetByTokenAsync(request.InternalOrderToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<GetInternalOrderByTokenQueryResponse>.FailureResponse(ErrorCodes.InternalOrderNotFound, "Internal order not found.", 404);

            return ApiResponse<GetInternalOrderByTokenQueryResponse>.SuccessResponse(new GetInternalOrderByTokenQueryResponse
            {
                InternalOrder = mapper.Map<Responses.Common.InternalOrder>(result)
            });
        }
    }
}
