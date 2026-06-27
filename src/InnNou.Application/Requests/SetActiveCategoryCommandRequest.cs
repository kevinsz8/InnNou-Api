using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record SetActiveCategoryCommandRequest(Guid CategoryToken, bool IsActive) : IRequest<ApiResponse<SetActiveCategoryCommandResponse>>;
}
