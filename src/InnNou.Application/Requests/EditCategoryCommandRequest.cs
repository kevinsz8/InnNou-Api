using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditCategoryCommandRequest : IRequest<ApiResponse<EditCategoryCommandResponse>>
    {
        public Guid CategoryToken { get; set; }
        public string Code { get; set; } = string.Empty;
    }
}
