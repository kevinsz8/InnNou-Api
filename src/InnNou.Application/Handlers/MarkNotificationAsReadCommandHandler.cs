using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class MarkNotificationAsReadCommandHandler(INotificationService notificationService, IRequestContext context)
        : IRequestHandler<MarkNotificationAsReadCommandRequest, ApiResponse<MarkNotificationAsReadCommandResponse>>
    {
        public async Task<ApiResponse<MarkNotificationAsReadCommandResponse>> Handle(MarkNotificationAsReadCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.NotificationToken == Guid.Empty)
                return ApiResponse<MarkNotificationAsReadCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "NotificationToken is required.", 400);

            await notificationService.MarkAsReadAsync(request.NotificationToken, context, cancellationToken);

            return ApiResponse<MarkNotificationAsReadCommandResponse>.SuccessResponse(new MarkNotificationAsReadCommandResponse());
        }
    }
}
