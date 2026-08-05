using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetRequisitionByTokenQueryRequest : IRequest<ApiResponse<GetRequisitionByTokenQueryResponse>>
    {
        public Guid RequisitionToken { get; set; }
    }
}
