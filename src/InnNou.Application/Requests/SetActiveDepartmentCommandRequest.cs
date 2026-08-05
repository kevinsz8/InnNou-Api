using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetActiveDepartmentCommandRequest : IRequest<ApiResponse<SetActiveDepartmentCommandResponse>>
    {
        public Guid DepartmentToken { get; set; }
        public bool IsActive { get; set; }
    }
}
