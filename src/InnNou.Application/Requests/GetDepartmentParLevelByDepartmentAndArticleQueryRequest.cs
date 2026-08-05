using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetDepartmentParLevelByDepartmentAndArticleQueryRequest : IRequest<ApiResponse<GetDepartmentParLevelByDepartmentAndArticleQueryResponse>>
    {
        public Guid DepartmentToken { get; set; }
        public Guid ArticleToken { get; set; }
    }
}
