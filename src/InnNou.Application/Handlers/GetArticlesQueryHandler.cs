using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetArticlesQueryHandler(IArticleService articleService, ISupplierService supplierService, IFamilyService familyService, ISubFamilyService subFamilyService, IOrganizationService organizationService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetArticlesQueryRequest, ApiResponse<GetArticlesQueryResponse>>
    {
        public async Task<ApiResponse<GetArticlesQueryResponse>> Handle(GetArticlesQueryRequest request, CancellationToken cancellationToken)
        {
            int? organizationId = null;
            if (request.OrganizationToken.HasValue)
            {
                // GetOrganizationByTokenAsync already returns null for an organization outside
                // the caller's own scope (hierarchy for Admin/Manager, exact match for Staff,
                // unrestricted for SuperAdmin/supplier) — reusing it here is what stops this
                // parameter from becoming a way to probe an arbitrary organization's favorites.
                var organization = await organizationService.GetOrganizationByTokenAsync(request.OrganizationToken.Value, context, cancellationToken);
                if (organization is null)
                    return ApiResponse<GetArticlesQueryResponse>.FailureResponse(ErrorCodes.OrganizationOutsideScope, "Organization not found or outside your scope.", 403);
                organizationId = organization.OrganizationId;
            }

            int? supplierId = null;
            if (request.SupplierToken.HasValue)
            {
                var supplier = await supplierService.GetSupplierByTokenAsync(request.SupplierToken.Value, context, cancellationToken);
                if (supplier is null)
                    return ApiResponse<GetArticlesQueryResponse>.FailureResponse(ErrorCodes.SupplierNotFound, "Supplier not found.", 404);
                supplierId = supplier.SupplierId;
            }

            int? familyId = null;
            if (request.FamilyToken.HasValue)
            {
                var family = await familyService.GetByTokenAsync(request.FamilyToken.Value, cancellationToken);
                if (family is null)
                    return ApiResponse<GetArticlesQueryResponse>.FailureResponse(ErrorCodes.FamilyNotFound, "Family not found.", 404);
                familyId = family.FamilyId;
            }

            int? subFamilyId = null;
            if (request.SubFamilyToken.HasValue)
            {
                var subFamily = await subFamilyService.GetByTokenAsync(request.SubFamilyToken.Value, cancellationToken);
                if (subFamily is null)
                    return ApiResponse<GetArticlesQueryResponse>.FailureResponse(ErrorCodes.SubFamilyNotFound, "Sub-family not found.", 404);
                subFamilyId = subFamily.SubFamilyId;
            }

            var result = await articleService.GetPagedAsync(request.PageNumber, request.PageSize, supplierId, familyId, subFamilyId, request.SearchText, request.IncludeInactive, request.FavoritesOnly, organizationId, context, cancellationToken);
            var totalPages = result.TotalPages;
            var response = new GetArticlesQueryResponse
            {
                Articles = mapper.MapList<Responses.Common.Article>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.PageNumber < totalPages,
                HasPreviousPage = request.PageNumber > 1,
                NextPageNumber = request.PageNumber < totalPages ? request.PageNumber + 1 : (int?)null,
                PreviousPageNumber = request.PageNumber > 1 ? request.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetArticlesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
