using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record EditCategoryCommandRequest(Guid CategoryToken, string Code) : IRequest<ApiResponse<EditCategoryCommandResponse>>;
}
