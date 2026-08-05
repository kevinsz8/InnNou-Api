using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetSuggestedRequisitionsQueryRequest : IRequest<ApiResponse<GetSuggestedRequisitionsQueryResponse>>
    {
        public Guid? OrganizationToken { get; set; }
        public Guid? DepartmentToken { get; set; }
        public Guid? ArticleToken { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
