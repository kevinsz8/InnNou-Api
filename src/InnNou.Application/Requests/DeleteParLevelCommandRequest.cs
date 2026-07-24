using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteParLevelCommandRequest : IRequest<ApiResponse<DeleteParLevelCommandResponse>>
    {
        public Guid ParLevelToken { get; set; }
    }
}
