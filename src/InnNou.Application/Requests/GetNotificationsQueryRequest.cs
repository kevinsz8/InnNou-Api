using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetNotificationsQueryRequest : IRequest<ApiResponse<GetNotificationsQueryResponse>>
    {
        public bool UnreadOnly { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
