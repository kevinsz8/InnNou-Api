using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetActiveWarehouseCommandRequest : IRequest<ApiResponse<SetActiveWarehouseCommandResponse>>
    {
        public Guid WarehouseToken { get; set; }
        public bool IsActive { get; set; }
    }
}
