using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetSubCategoryByTokenQueryRequest : IRequest<ApiResponse<GetSubCategoryByTokenQueryResponse>>
    {
        public Guid SubCategoryToken { get; set; }
    }
}
