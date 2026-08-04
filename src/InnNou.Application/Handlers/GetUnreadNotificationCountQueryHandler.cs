using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetUnreadNotificationCountQueryHandler(INotificationService notificationService, IRequestContext context)
        : IRequestHandler<GetUnreadNotificationCountQueryRequest, ApiResponse<GetUnreadNotificationCountQueryResponse>>
    {
        public async Task<ApiResponse<GetUnreadNotificationCountQueryResponse>> Handle(GetUnreadNotificationCountQueryRequest request, CancellationToken cancellationToken)
        {
            var count = await notificationService.GetUnreadCountAsync(context, cancellationToken);

            return ApiResponse<GetUnreadNotificationCountQueryResponse>.SuccessResponse(new GetUnreadNotificationCountQueryResponse
            {
                UnreadCount = count
            });
        }
    }
}
