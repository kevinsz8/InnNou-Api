using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetArticleDiscountsQueryRequest : IRequest<ApiResponse<GetArticleDiscountsQueryResponse>>
    {
        public Guid SupplierToken { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public bool IncludeInactive { get; set; } = false;
    }
}
