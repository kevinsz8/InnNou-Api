using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditArticleDiscountCommandRequest : IRequest<ApiResponse<EditArticleDiscountCommandResponse>>
    {
        public Guid ArticleDiscountToken { get; set; }
        public string DiscountTypeCode { get; set; } = default!;
        public decimal DiscountValue { get; set; }
        public string? CurrencyCode { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveUntil { get; set; }
        public string? Description { get; set; }
    }
}
