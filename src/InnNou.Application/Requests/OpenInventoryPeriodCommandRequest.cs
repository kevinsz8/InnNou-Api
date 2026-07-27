using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class OpenInventoryPeriodCommandRequest : IRequest<ApiResponse<OpenInventoryPeriodCommandResponse>>
    {
        public Guid WarehouseToken { get; set; }
        public string? Notes { get; set; }
    }
}
