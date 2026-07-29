using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetInventoryTransfersQueryHandler(
        IInventoryService inventoryService,
        IOrganizationService organizationService,
        IFamilyService familyService,
        ISubFamilyService subFamilyService,
        ICategoryService categoryService,
        ISubCategoryService subCategoryService,
        IMapper mapper,
        IRequestContext context)
        : IRequestHandler<GetInventoryTransfersQueryRequest, ApiResponse<GetInventoryTransfersQueryResponse>>
    {
        public async Task<ApiResponse<GetInventoryTransfersQueryResponse>> Handle(GetInventoryTransfersQueryRequest request, CancellationToken cancellationToken)
        {
            int? organizationId = null;
            if (request.OrganizationToken.HasValue)
            {
                // Same "already scoped by the caller's own hierarchy" reuse GetArticlesQueryHandler
                // established — this can never widen visibility beyond what the caller could
                // already see, it only lets a bare SuperAdmin narrow the global default.
                var organization = await organizationService.GetOrganizationByTokenAsync(request.OrganizationToken.Value, context, cancellationToken);
                if (organization is null)
                    return ApiResponse<GetInventoryTransfersQueryResponse>.FailureResponse(ErrorCodes.OrganizationOutsideScope, "Organization not found or outside your scope.", 403);
                organizationId = organization.OrganizationId;
            }

            int? familyId = null;
            if (request.FamilyToken.HasValue)
            {
                var family = await familyService.GetByTokenAsync(request.FamilyToken.Value, cancellationToken);
                if (family is null)
                    return ApiResponse<GetInventoryTransfersQueryResponse>.FailureResponse(ErrorCodes.FamilyNotFound, "Family not found.", 404);
                familyId = family.FamilyId;
            }

            int? subFamilyId = null;
            if (request.SubFamilyToken.HasValue)
            {
                var subFamily = await subFamilyService.GetByTokenAsync(request.SubFamilyToken.Value, cancellationToken);
                if (subFamily is null)
                    return ApiResponse<GetInventoryTransfersQueryResponse>.FailureResponse(ErrorCodes.SubFamilyNotFound, "Sub-family not found.", 404);
                subFamilyId = subFamily.SubFamilyId;
            }

            int? categoryId = null;
            if (request.CategoryToken.HasValue)
            {
                var category = await categoryService.GetByTokenAsync(request.CategoryToken.Value, context, cancellationToken);
                if (category is null)
                    return ApiResponse<GetInventoryTransfersQueryResponse>.FailureResponse(ErrorCodes.CategoryNotFound, "Category not found.", 404);
                categoryId = category.CategoryId;
            }

            int? subCategoryId = null;
            if (request.SubCategoryToken.HasValue)
            {
                var subCategory = await subCategoryService.GetByTokenAsync(request.SubCategoryToken.Value, context, cancellationToken);
                if (subCategory is null)
                    return ApiResponse<GetInventoryTransfersQueryResponse>.FailureResponse(ErrorCodes.SubCategoryNotFound, "Sub-category not found.", 404);
                subCategoryId = subCategory.SubCategoryId;
            }

            var result = await inventoryService.GetTransfersAsync(
                request.WarehouseToken, organizationId, request.FromDate, request.ToDate, request.ArticleToken,
                familyId, subFamilyId, categoryId, subCategoryId,
                request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetInventoryTransfersQueryResponse
            {
                InventoryTransfers = mapper.MapList<Responses.Common.InventoryTransfer>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetInventoryTransfersQueryResponse>.SuccessResponse(response);
        }
    }
}
