using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetOrderByTokenQueryRequest : IRequest<ApiResponse<GetOrderByTokenQueryResponse>>
    {
        public Guid OrderToken { get; set; }
    }
}
