using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class UnassignArticleClassificationCommandRequest : IRequest<ApiResponse<UnassignArticleClassificationCommandResponse>>
    {
        public Guid ArticleToken { get; set; }
        public Guid? OrganizationToken { get; set; }
    }
}
