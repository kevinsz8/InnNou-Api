using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateArticlePriceCommandRequest : IRequest<ApiResponse<CreateArticlePriceCommandResponse>>
    {
        public Guid ArticleToken { get; set; }
        public Guid? OrganizationToken { get; set; }
        public decimal Price { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public DateTime? EffectiveDate { get; set; }
        public string? Notes { get; set; }
    }
}
