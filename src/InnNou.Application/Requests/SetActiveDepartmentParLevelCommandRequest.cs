using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetActiveDepartmentParLevelCommandRequest : IRequest<ApiResponse<SetActiveDepartmentParLevelCommandResponse>>
    {
        public Guid DepartmentParLevelToken { get; set; }
        public bool IsActive { get; set; }
    }
}
