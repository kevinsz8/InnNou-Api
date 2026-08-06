using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateSupplierCreditNoteLineInput
    {
        public Guid SupplierReturnLineToken { get; set; }
        // Optional override — omitted, it defaults to the underlying GoodsReceiptLine's own
        // frozen UnitPrice; required only when that line predates unit-price freezing.
        public decimal? UnitPrice { get; set; }
    }

    public class CreateSupplierCreditNoteCommandRequest : IRequest<ApiResponse<CreateSupplierCreditNoteCommandResponse>>
    {
        public Guid SupplierReturnToken { get; set; }
        public string CreditNoteNumber { get; set; } = default!;
        public DateTime CreditNoteDate { get; set; }
        public string Reason { get; set; } = default!;
        public string? Notes { get; set; }
        public List<CreateSupplierCreditNoteLineInput> Lines { get; set; } = [];
    }
}
