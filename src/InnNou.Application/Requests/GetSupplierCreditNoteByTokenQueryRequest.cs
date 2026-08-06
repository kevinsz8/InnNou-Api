using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetSupplierCreditNoteByTokenQueryRequest : IRequest<ApiResponse<GetSupplierCreditNoteByTokenQueryResponse>>
    {
        public Guid SupplierCreditNoteToken { get; set; }
    }

    // Used by the SupplierReturn detail page to link out to an already-registered credit note
    // (or offer to create one) without needing its token up front.
    public class GetSupplierCreditNoteBySupplierReturnTokenQueryRequest : IRequest<ApiResponse<GetSupplierCreditNoteByTokenQueryResponse>>
    {
        public Guid SupplierReturnToken { get; set; }
    }
}
