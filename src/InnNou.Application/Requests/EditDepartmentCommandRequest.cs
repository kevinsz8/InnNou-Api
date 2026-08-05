using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditDepartmentCommandRequest : IRequest<ApiResponse<EditDepartmentCommandResponse>>
    {
        public Guid DepartmentToken { get; set; }
        public string Name { get; set; } = default!;
        public string? Code { get; set; }
    }
}
