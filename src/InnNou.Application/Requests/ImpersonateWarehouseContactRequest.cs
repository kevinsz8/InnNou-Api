using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class ImpersonateWarehouseContactRequest : IRequest<ApiResponse<ImpersonateResponse>>
    {
        public Guid WarehouseContactToken { get; set; }
    }
}
