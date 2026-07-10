using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetCurrentArticlePriceQueryRequest : IRequest<ApiResponse<GetCurrentArticlePriceQueryResponse>>
    {
        public Guid ArticleToken { get; set; }
        // Only honored for the owning supplier/Admin — a regular caller always resolves for their own organization.
        public Guid? OrganizationToken { get; set; }
        public string? CurrencyCode { get; set; }
        public DateTime? AsOfDate { get; set; }
    }
}
