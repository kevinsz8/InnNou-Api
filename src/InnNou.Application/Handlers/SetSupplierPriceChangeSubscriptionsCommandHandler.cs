using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetSupplierPriceChangeSubscriptionsCommandHandler(ISupplierPriceSubscriptionService subscriptionService, IMapper mapper, IRequestContext context)
        : IRequestHandler<SetSupplierPriceChangeSubscriptionsCommandRequest, ApiResponse<SetSupplierPriceChangeSubscriptionsCommandResponse>>
    {
        public async Task<ApiResponse<SetSupplierPriceChangeSubscriptionsCommandResponse>> Handle(SetSupplierPriceChangeSubscriptionsCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.SupplierTokens is null)
                return ApiResponse<SetSupplierPriceChangeSubscriptionsCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "SupplierTokens is required.", 400);

            var subscriptions = await subscriptionService.SetSubscriptionsAsync(request.SupplierTokens, context, cancellationToken);

            var response = new SetSupplierPriceChangeSubscriptionsCommandResponse
            {
                Subscriptions = mapper.MapList<Responses.Common.SupplierPriceChangeSubscription>(subscriptions)
            };
            return ApiResponse<SetSupplierPriceChangeSubscriptionsCommandResponse>.SuccessResponse(response);
        }
    }
}
