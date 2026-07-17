using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateOrderCommandRequest : IRequest<ApiResponse<CreateOrderCommandResponse>>
    {
        public Guid WarehouseToken { get; set; }
        public string? Notes { get; set; }
    }
}
