using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetActiveSubCategoryCommandRequest : IRequest<ApiResponse<SetActiveSubCategoryCommandResponse>>
    {
        public Guid SubCategoryToken { get; set; }
        public bool IsActive { get; set; }
    }
}
