using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record SetActiveSubCategoryCommandRequest(Guid SubCategoryToken, bool IsActive) : IRequest<ApiResponse<SetActiveSubCategoryCommandResponse>>;
}
