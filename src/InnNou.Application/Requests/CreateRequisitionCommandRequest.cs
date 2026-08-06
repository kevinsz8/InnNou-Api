using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateRequisitionLineRequestItem
    {
        public Guid ArticleToken { get; set; }
        public decimal QuantityRequested { get; set; }
        public Guid? UnitToken { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateRequisitionCommandRequest : IRequest<ApiResponse<CreateRequisitionCommandResponse>>
    {
        public Guid WarehouseToken { get; set; }
        public Guid DepartmentToken { get; set; }
        public string? Notes { get; set; }
        public List<CreateRequisitionLineRequestItem> Lines { get; set; } = [];
    }
}
