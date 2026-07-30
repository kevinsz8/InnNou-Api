using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetSupplierReturnByTokenQueryRequest : IRequest<ApiResponse<GetSupplierReturnByTokenQueryResponse>>
    {
        public Guid SupplierReturnToken { get; set; }
    }
}
