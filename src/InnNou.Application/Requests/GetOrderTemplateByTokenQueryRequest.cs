using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetOrderTemplateByTokenQueryRequest : IRequest<ApiResponse<GetOrderTemplateByTokenQueryResponse>>
    {
        public Guid OrderTemplateToken { get; set; }
    }
}
