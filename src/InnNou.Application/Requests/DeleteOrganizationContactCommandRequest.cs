using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteOrganizationContactCommandRequest : IRequest<ApiResponse<DeleteOrganizationContactCommandResponse>>
    {
        public Guid OrganizationContactToken { get; set; }
    }
}
