using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteWarehouseContactCommandRequest : IRequest<ApiResponse<DeleteWarehouseContactCommandResponse>>
    {
        public Guid WarehouseContactToken { get; set; }
    }
}
