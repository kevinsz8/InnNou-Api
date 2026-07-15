using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetWarehouseByTokenQueryRequest : IRequest<ApiResponse<GetWarehouseByTokenQueryResponse>>
    {
        public Guid WarehouseToken { get; set; }
    }
}
