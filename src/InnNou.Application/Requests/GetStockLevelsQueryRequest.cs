using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetStockLevelsQueryRequest : IRequest<ApiResponse<GetStockLevelsQueryResponse>>
    {
        public Guid? WarehouseToken { get; set; }
        public Guid? ArticleToken { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
