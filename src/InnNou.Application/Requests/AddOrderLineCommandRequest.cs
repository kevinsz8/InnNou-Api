using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class AddOrderLineCommandRequest : IRequest<ApiResponse<AddOrderLineCommandResponse>>
    {
        public Guid OrderToken { get; set; }
        public Guid ArticleToken { get; set; }
        public decimal Quantity { get; set; }

        // Only honored when the article's supplier is SERVICE/MIXED and it has no
        // catalog ArticlePrice — see CLAUDE.md's "Supplier type" section. Ignored (and
        // not required) for PRODUCT suppliers, which must resolve a real catalog price.
        public decimal? ManualUnitPrice { get; set; }
        public string? ManualCurrencyCode { get; set; }
    }
}
