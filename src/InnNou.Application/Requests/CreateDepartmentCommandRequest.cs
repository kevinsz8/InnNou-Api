using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateDepartmentCommandRequest : IRequest<ApiResponse<CreateDepartmentCommandResponse>>
    {
        public Guid OrganizationToken { get; set; }
        public string Name { get; set; } = default!;
        public string? Code { get; set; }
    }
}
