using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CloseInventoryPeriodCommandRequest : IRequest<ApiResponse<CloseInventoryPeriodCommandResponse>>
    {
        public Guid InventoryPeriodToken { get; set; }
    }
}
