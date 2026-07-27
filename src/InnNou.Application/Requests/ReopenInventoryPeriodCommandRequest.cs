using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class ReopenInventoryPeriodCommandRequest : IRequest<ApiResponse<ReopenInventoryPeriodCommandResponse>>
    {
        public Guid InventoryPeriodToken { get; set; }
    }
}
