using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record EditSubCategoryCommandRequest(Guid SubCategoryToken, string Code) : IRequest<ApiResponse<EditSubCategoryCommandResponse>>;
}
