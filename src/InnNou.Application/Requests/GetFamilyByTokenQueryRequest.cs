using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetFamilyByTokenQueryRequest : IRequest<ApiResponse<GetFamilyByTokenQueryResponse>>
    {
        public Guid FamilyToken { get; set; }
    }
}
