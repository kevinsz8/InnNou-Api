using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    // No params — always the caller's own effective identity.
    public class GetUnreadNotificationCountQueryRequest : IRequest<ApiResponse<GetUnreadNotificationCountQueryResponse>>
    {
    }
}
