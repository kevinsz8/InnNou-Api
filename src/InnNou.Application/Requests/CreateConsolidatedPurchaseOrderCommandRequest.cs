using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateConsolidatedPurchaseOrderCommandRequest : IRequest<ApiResponse<CreateConsolidatedPurchaseOrderCommandResponse>>
    {
        public Guid SupplierToken { get; set; }
        public Guid? SuperAssociateOrganizationToken { get; set; }
        public string? Title { get; set; }
        public string? Notes { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public List<Guid> PurchaseOrderTokens { get; set; } = [];
    }
}
