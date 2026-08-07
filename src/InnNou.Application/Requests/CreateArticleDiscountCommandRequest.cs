using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateArticleDiscountCommandRequest : IRequest<ApiResponse<CreateArticleDiscountCommandResponse>>
    {
        public Guid SupplierToken { get; set; }
        public Guid? ArticleToken { get; set; }
        public Guid? SubFamilyToken { get; set; }
        public Guid? FamilyToken { get; set; }
        public string DiscountTypeCode { get; set; } = default!;
        public decimal DiscountValue { get; set; }
        public string? CurrencyCode { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveUntil { get; set; }
        public string? Description { get; set; }
    }
}
