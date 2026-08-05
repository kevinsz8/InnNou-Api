using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateRequisitionIssueLineRequestItem
    {
        public Guid RequisitionLineToken { get; set; }
        public decimal QuantityIssued { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateRequisitionIssueCommandRequest : IRequest<ApiResponse<CreateRequisitionIssueCommandResponse>>
    {
        public Guid RequisitionToken { get; set; }
        public string? Notes { get; set; }
        public List<CreateRequisitionIssueLineRequestItem> Lines { get; set; } = [];
    }
}
