using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditDepartmentParLevelCommandRequest : IRequest<ApiResponse<EditDepartmentParLevelCommandResponse>>
    {
        public Guid DepartmentParLevelToken { get; set; }
        public decimal MinimumQuantity { get; set; }
        public decimal ReorderQuantity { get; set; }
    }
}
