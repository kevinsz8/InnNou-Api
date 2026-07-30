using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class UpsertSupplierInvoiceMatchToleranceCommandResponse
    {
        public SupplierInvoiceMatchTolerance Tolerance { get; set; } = default!;
    }
}
