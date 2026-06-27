using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record GetSubCategoryByTokenQueryRequest(Guid SubCategoryToken) : IRequest<ApiResponse<GetSubCategoryByTokenQueryResponse>>;
}
