using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetInventoryTransfersQueryRequest : IRequest<ApiResponse<GetInventoryTransfersQueryResponse>>
    {
        public Guid? WarehouseToken { get; set; }
        // Lets a bare (non-impersonated) SuperAdmin session — which has no OrganizationId of its
        // own — scope the search to one specific organization instead of the global unrestricted
        // default, mirroring GetArticlesQueryRequest's own OrganizationToken override. Resolved via
        // IOrganizationService.GetOrganizationByTokenAsync, which already returns null for an
        // organization outside a non-SuperAdmin caller's own hierarchy — this can never be used to
        // widen visibility beyond what the caller could already see.
        public Guid? OrganizationToken { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public Guid? ArticleToken { get; set; }
        public Guid? FamilyToken { get; set; }
        public Guid? SubFamilyToken { get; set; }
        public Guid? CategoryToken { get; set; }
        public Guid? SubCategoryToken { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
