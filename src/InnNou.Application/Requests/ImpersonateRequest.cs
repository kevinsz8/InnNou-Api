using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class ImpersonateRequest : IRequest<ApiResponse<ImpersonateResponse>>
    {
        public Guid TargetUserToken { get; set; }
    }
}
