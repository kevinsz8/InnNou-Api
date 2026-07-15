using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteWarehouseCommandRequest : IRequest<ApiResponse<DeleteWarehouseCommandResponse>>
    {
        public Guid WarehouseToken { get; set; }
    }
}
