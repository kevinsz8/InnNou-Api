using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetSupplierByTokenQueryRequest : IRequest<ApiResponse<GetSupplierByTokenQueryResponse>>
    {
        public Guid SupplierToken { get; set; }
    }
}
