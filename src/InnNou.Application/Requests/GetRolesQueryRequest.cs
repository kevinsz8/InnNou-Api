using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetRolesQueryRequest : IRequest<ApiResponse<GetRolesQueryResponse>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchField { get; set; }
        public string? SearchText { get; set; }
    }
}