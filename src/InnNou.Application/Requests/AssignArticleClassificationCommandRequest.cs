using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class AssignArticleClassificationCommandRequest : IRequest<ApiResponse<AssignArticleClassificationCommandResponse>>
    {
        public Guid ArticleToken { get; set; }
        public Guid CategoryToken { get; set; }
        public Guid? SubCategoryToken { get; set; }

        // Optional: only meaningful for SuperAdmin — targets a Super Asociado organization other
        // than the caller's own. Ignored for a Super Asociado's own Staff+ caller (forced to their
        // own organization), same convention as CategoryDto.OrganizationToken on create.
        public Guid? OrganizationToken { get; set; }
    }
}
