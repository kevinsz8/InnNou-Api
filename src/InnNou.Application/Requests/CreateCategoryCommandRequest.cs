using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateCategoryCommandRequest : IRequest<ApiResponse<CreateCategoryCommandResponse>>
    {
        public string Code { get; set; } = string.Empty;
    }
}
