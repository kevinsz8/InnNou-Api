using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetSupplierInvoiceMatchToleranceQueryResponse
    {
        // Null when no tolerance is configured anywhere in the organization's ancestry.
        public SupplierInvoiceMatchTolerance? Tolerance { get; set; }
    }
}
