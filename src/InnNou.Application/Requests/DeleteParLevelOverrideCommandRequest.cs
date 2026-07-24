using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteParLevelOverrideCommandRequest : IRequest<ApiResponse<DeleteParLevelOverrideCommandResponse>>
    {
        public Guid ParLevelOverrideToken { get; set; }
    }
}
