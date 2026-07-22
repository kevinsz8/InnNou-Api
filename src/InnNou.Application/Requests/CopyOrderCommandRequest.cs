using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CopyOrderCommandRequest : IRequest<ApiResponse<CopyOrderCommandResponse>>
    {
        public Guid OrderToken { get; set; }
    }
}
