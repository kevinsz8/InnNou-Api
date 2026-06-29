using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetCategoryByTokenQueryRequest : IRequest<ApiResponse<GetCategoryByTokenQueryResponse>>
    {
        public Guid CategoryToken { get; set; }
    }
}
