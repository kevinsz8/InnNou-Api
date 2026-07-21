using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class BulkAssignArticleClassificationCommandRequest : IRequest<ApiResponse<BulkAssignArticleClassificationCommandResponse>>
    {
        public List<Guid> ArticleTokens { get; set; } = [];
        public Guid CategoryToken { get; set; }
        public Guid? SubCategoryToken { get; set; }
        public Guid? OrganizationToken { get; set; }
    }
}
