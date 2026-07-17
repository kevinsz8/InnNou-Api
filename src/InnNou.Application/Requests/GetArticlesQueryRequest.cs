using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetArticlesQueryRequest : IRequest<ApiResponse<GetArticlesQueryResponse>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? SupplierToken { get; set; }
        public Guid? FamilyToken { get; set; }
        public Guid? SubFamilyToken { get; set; }
        public string? SearchText { get; set; }
        public bool IncludeInactive { get; set; } = false;
        public bool FavoritesOnly { get; set; } = false;

        // Optional: compute IsFavorite/@FavoritesOnly against a specific organization
        // instead of the caller's own session org — e.g. an Admin/SuperAdmin building an
        // Order for a different organization's warehouse. The handler authorizes this via
        // IOrganizationService.GetOrganizationByTokenAsync's existing hierarchy scoping
        // before it's used, same as any other organization-scoped read in this codebase.
        public Guid? OrganizationToken { get; set; }
    }
}
