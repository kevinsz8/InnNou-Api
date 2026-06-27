using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record GetCategoryByTokenQueryRequest(Guid CategoryToken) : IRequest<ApiResponse<GetCategoryByTokenQueryResponse>>;
}
