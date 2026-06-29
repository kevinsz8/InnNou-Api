using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetActiveCategoryCommandRequest : IRequest<ApiResponse<SetActiveCategoryCommandResponse>>
    {
        public Guid CategoryToken { get; set; }
        public bool IsActive { get; set; }
    }
}
