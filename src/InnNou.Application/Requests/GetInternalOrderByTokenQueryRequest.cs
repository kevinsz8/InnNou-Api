using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetInternalOrderByTokenQueryRequest : IRequest<ApiResponse<GetInternalOrderByTokenQueryResponse>>
    {
        public Guid InternalOrderToken { get; set; }
    }
}
