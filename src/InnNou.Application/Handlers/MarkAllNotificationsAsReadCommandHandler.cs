using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class MarkAllNotificationsAsReadCommandHandler(INotificationService notificationService, IRequestContext context)
        : IRequestHandler<MarkAllNotificationsAsReadCommandRequest, ApiResponse<MarkAllNotificationsAsReadCommandResponse>>
    {
        public async Task<ApiResponse<MarkAllNotificationsAsReadCommandResponse>> Handle(MarkAllNotificationsAsReadCommandRequest request, CancellationToken cancellationToken)
        {
            await notificationService.MarkAllAsReadAsync(context, cancellationToken);

            return ApiResponse<MarkAllNotificationsAsReadCommandResponse>.SuccessResponse(new MarkAllNotificationsAsReadCommandResponse());
        }
    }
}
