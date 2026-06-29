using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetSubFamilyByTokenQueryRequest : IRequest<ApiResponse<GetSubFamilyByTokenQueryResponse>>
    {
        public Guid SubFamilyToken { get; set; }
    }
}
