using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetZoneByTokenQueryRequest : IRequest<ApiResponse<GetZoneByTokenQueryResponse>>
    {
        public Guid ZoneToken { get; set; }
    }
}
