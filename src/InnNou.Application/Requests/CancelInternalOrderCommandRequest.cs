using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CancelInternalOrderCommandRequest : IRequest<ApiResponse<CancelInternalOrderCommandResponse>>
    {
        public Guid InternalOrderToken { get; set; }
        public string Reason { get; set; } = default!;
    }
}
