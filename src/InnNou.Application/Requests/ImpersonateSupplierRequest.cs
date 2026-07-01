using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class ImpersonateSupplierRequest : IRequest<ApiResponse<ImpersonateResponse>>
    {
        public Guid SupplierToken { get; set; }
    }
}
