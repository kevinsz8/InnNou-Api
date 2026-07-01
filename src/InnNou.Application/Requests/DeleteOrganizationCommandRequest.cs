using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteOrganizationCommandRequest : IRequest<ApiResponse<DeleteOrganizationCommandResponse>>
    {
        public Guid OrganizationToken { get; set; }
    }
}
