using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteSupplierCommandRequest : IRequest<ApiResponse<DeleteSupplierCommandResponse>>
    {
        public Guid SupplierToken { get; set; }
    }
}
