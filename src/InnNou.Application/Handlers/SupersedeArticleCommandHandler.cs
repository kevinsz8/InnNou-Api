using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SupersedeArticleCommandHandler(IArticleService articleService, IUnitOfMeasureService unitOfMeasureService, IFamilyService familyService, ISubFamilyService subFamilyService, IMapper mapper, IRequestContext context)
        : IRequestHandler<SupersedeArticleCommandRequest, ApiResponse<SupersedeArticleCommandResponse>>
    {
        public async Task<ApiResponse<SupersedeArticleCommandResponse>> Handle(SupersedeArticleCommandRequest request, CancellationToken cancellationToken)
        {
            var existing = await articleService.GetByTokenAsync(request.ArticleToken, context, cancellationToken);
            if (existing is null)
                return ApiResponse<SupersedeArticleCommandResponse>.FailureResponse(ErrorCodes.ArticleNotFound, "Article not found.", 404);

            if (existing.ReplacedByArticleId.HasValue)
                return ApiResponse<SupersedeArticleCommandResponse>.FailureResponse(ErrorCodes.ArticleAlreadyReplaced, "This article has already been replaced.", 409);

            var purchaseUnit = await unitOfMeasureService.GetByTokenAsync(request.PurchaseUnitToken, cancellationToken);
            if (purchaseUnit is null)
                return ApiResponse<SupersedeArticleCommandResponse>.FailureResponse(ErrorCodes.PurchaseUnitNotFound, "Purchase unit of measure not found.", 404);
            if (!string.Equals(purchaseUnit.UnitTypeCode, UnitTypeCodes.Count, StringComparison.OrdinalIgnoreCase))
                return ApiResponse<SupersedeArticleCommandResponse>.FailureResponse(ErrorCodes.PurchaseUnitInvalidType, "Purchase unit must be a COUNT unit (e.g. BOX, PACK, BAG).", 400);

            var levelsResult = await ArticlePackagingLevelValidation.ResolveAsync(request.PackagingLevels, unitOfMeasureService, cancellationToken);
            if (levelsResult.Error is not null)
                return ApiResponse<SupersedeArticleCommandResponse>.FailureResponse(levelsResult.Error.Code, levelsResult.Error.Description, levelsResult.Error.StatusCode);

            var isStructuralChange =
                purchaseUnit.UnitOfMeasureId != existing.PurchaseUnitId ||
                !ArticlePackagingLevelValidation.AreEqual(levelsResult.Levels, existing.PackagingLevels);

            if (!isStructuralChange)
                return ApiResponse<SupersedeArticleCommandResponse>.FailureResponse(
                    ErrorCodes.NoStructuralChange,
                    "Nothing about the article's structure changed — use the regular edit for non-structural changes.",
                    400);

            int? familyId = null;
            if (request.FamilyToken.HasValue)
            {
                var family = await familyService.GetByTokenAsync(request.FamilyToken.Value, cancellationToken);
                if (family is null)
                    return ApiResponse<SupersedeArticleCommandResponse>.FailureResponse(ErrorCodes.FamilyNotFound, "Family not found.", 404);
                familyId = family.FamilyId;
            }

            int? subFamilyId = null;
            if (request.SubFamilyToken.HasValue)
            {
                var subFamily = await subFamilyService.GetByTokenAsync(request.SubFamilyToken.Value, cancellationToken);
                if (subFamily is null)
                    return ApiResponse<SupersedeArticleCommandResponse>.FailureResponse(ErrorCodes.SubFamilyNotFound, "Sub-family not found.", 404);
                subFamilyId = subFamily.SubFamilyId;
            }

            if (!string.IsNullOrWhiteSpace(request.SupplierSku))
            {
                var skuExists = await articleService.ExistsBySupplierSkuAsync(existing.SupplierId, request.SupplierSku, request.ArticleToken, cancellationToken);
                if (skuExists)
                    return ApiResponse<SupersedeArticleCommandResponse>.FailureResponse(ErrorCodes.ArticleSkuExists, "An article with this SKU already exists for this supplier.", 409);
            }

            var newArticleData = new ArticleDto
            {
                SupplierId = existing.SupplierId,
                Name = request.Name,
                Description = request.Description,
                SupplierSku = request.SupplierSku,
                Barcode = request.Barcode,
                Brand = request.Brand,
                FamilyId = familyId,
                SubFamilyId = subFamilyId,
                PurchaseUnitId = purchaseUnit.UnitOfMeasureId,
                PackagingLevels = levelsResult.Levels,
                MinimumOrderQty = request.MinimumOrderQty,
                LeadTimeDays = request.LeadTimeDays
            };

            var result = await articleService.SupersedeAsync(request.ArticleToken, newArticleData, context, cancellationToken);
            if (result is null)
                return ApiResponse<SupersedeArticleCommandResponse>.FailureResponse(ErrorCodes.ArticleSupersedeFailed, "Article could not be superseded.", 500);

            return ApiResponse<SupersedeArticleCommandResponse>.SuccessResponse(
                new SupersedeArticleCommandResponse
                {
                    ReplacedArticleToken = request.ArticleToken,
                    Article = mapper.Map<Responses.Common.Article>(result)
                }, 201);
        }
    }
}
