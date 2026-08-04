using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetMySupplierPriceChangeSubscriptionsQueryHandler(ISupplierPriceSubscriptionService subscriptionService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetMySupplierPriceChangeSubscriptionsQueryRequest, ApiResponse<GetMySupplierPriceChangeSubscriptionsQueryResponse>>
    {
        public async Task<ApiResponse<GetMySupplierPriceChangeSubscriptionsQueryResponse>> Handle(GetMySupplierPriceChangeSubscriptionsQueryRequest request, CancellationToken cancellationToken)
        {
            var subscriptions = await subscriptionService.GetMySubscriptionsAsync(context, cancellationToken);

            var response = new GetMySupplierPriceChangeSubscriptionsQueryResponse
            {
                Subscriptions = mapper.MapList<Responses.Common.SupplierPriceChangeSubscription>(subscriptions)
            };
            return ApiResponse<GetMySupplierPriceChangeSubscriptionsQueryResponse>.SuccessResponse(response);
        }
    }
}
