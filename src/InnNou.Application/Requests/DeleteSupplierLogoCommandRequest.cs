using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteSupplierLogoCommandRequest : IRequest<ApiResponse<DeleteSupplierLogoCommandResponse>>
    {
        public Guid SupplierToken { get; set; }
    }
}
