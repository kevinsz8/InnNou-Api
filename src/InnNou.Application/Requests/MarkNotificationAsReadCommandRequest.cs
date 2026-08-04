using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class MarkNotificationAsReadCommandRequest : IRequest<ApiResponse<MarkNotificationAsReadCommandResponse>>
    {
        public Guid NotificationToken { get; set; }
    }
}
