using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateCategoryCommandRequest : IRequest<ApiResponse<CreateCategoryCommandResponse>>
    {
        public string Code { get; set; } = string.Empty;

        // Only honored for a SuperAdmin caller — targets the category's owning
        // organization. Ignored for a Super Asociado's own Staff+, who always get
        // anchored to their own organization regardless of what's sent here.
        public Guid? OrganizationToken { get; set; }
    }
}
