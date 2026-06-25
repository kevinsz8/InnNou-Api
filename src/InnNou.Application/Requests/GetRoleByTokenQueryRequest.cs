using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetRoleByTokenQueryRequest : IRequest<ApiResponse<GetRoleByTokenQueryResponse>>
    {
        public Guid RoleToken { get; set; }
    }
}
