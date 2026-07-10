using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetArticlePriceHistoryQueryRequest : IRequest<ApiResponse<GetArticlePriceHistoryQueryResponse>>
    {
        public Guid ArticleToken { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        // Only honored for the owning supplier/Admin — a regular caller always sees their own organization plus global prices.
        public Guid? OrganizationToken { get; set; }
        public string? CurrencyCode { get; set; }
    }
}
