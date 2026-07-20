using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateArticleCommandHandler(IArticleService articleService, ISupplierService supplierService, IUnitOfMeasureService unitOfMeasureService, IFamilyService familyService, ISubFamilyService subFamilyService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateArticleCommandRequest, ApiResponse<CreateArticleCommandResponse>>
    {
        public async Task<ApiResponse<CreateArticleCommandResponse>> Handle(CreateArticleCommandRequest request, CancellationToken cancellationToken)
        {
            var supplier = await supplierService.GetSupplierByTokenAsync(request.SupplierToken, context, cancellationToken);
            if (supplier is null)
                return ApiResponse<CreateArticleCommandResponse>.FailureResponse(ErrorCodes.SupplierNotFound, "Supplier not found.", 404);

            var purchaseUnit = await unitOfMeasureService.GetByTokenAsync(request.PurchaseUnitToken, cancellationToken);
            if (purchaseUnit is null)
                return ApiResponse<CreateArticleCommandResponse>.FailureResponse(ErrorCodes.PurchaseUnitNotFound, "Purchase unit of measure not found.", 404);
            if (!string.Equals(purchaseUnit.UnitTypeCode, UnitTypeCodes.Count, StringComparison.OrdinalIgnoreCase))
                return ApiResponse<CreateArticleCommandResponse>.FailureResponse(ErrorCodes.PurchaseUnitInvalidType, "Purchase unit must be a COUNT unit (e.g. BOX, PACK, BAG).", 400);

            var levelsResult = await ArticlePackagingLevelValidation.ResolveAsync(request.PackagingLevels, unitOfMeasureService, cancellationToken);
            if (levelsResult.Error is not null)
                return ApiResponse<CreateArticleCommandResponse>.FailureResponse(levelsResult.Error.Code, levelsResult.Error.Description, levelsResult.Error.StatusCode);

            int? familyId = null;
            if (request.FamilyToken.HasValue)
            {
                var family = await familyService.GetByTokenAsync(request.FamilyToken.Value, cancellationToken);
                if (family is null)
                    return ApiResponse<CreateArticleCommandResponse>.FailureResponse(ErrorCodes.FamilyNotFound, "Family not found.", 404);
                familyId = family.FamilyId;
            }

            int? subFamilyId = null;
            if (request.SubFamilyToken.HasValue)
            {
                var subFamily = await subFamilyService.GetByTokenAsync(request.SubFamilyToken.Value, cancellationToken);
                if (subFamily is null)
                    return ApiResponse<CreateArticleCommandResponse>.FailureResponse(ErrorCodes.SubFamilyNotFound, "Sub-family not found.", 404);
                subFamilyId = subFamily.SubFamilyId;
            }

            if (!string.IsNullOrWhiteSpace(request.SupplierSku))
            {
                var skuExists = await articleService.ExistsBySupplierSkuAsync(supplier.SupplierId, request.SupplierSku, null, cancellationToken);
                if (skuExists)
                    return ApiResponse<CreateArticleCommandResponse>.FailureResponse(ErrorCodes.ArticleSkuExists, "An article with this SKU already exists for this supplier.", 409);
            }

            var dto = new ArticleDto
            {
                SupplierId = supplier.SupplierId,
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

            var result = await articleService.CreateAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateArticleCommandResponse>.FailureResponse(ErrorCodes.ArticleCreateFailed, "Article could not be created.", 500);

            return ApiResponse<CreateArticleCommandResponse>.SuccessResponse(
                new CreateArticleCommandResponse { Article = mapper.Map<Responses.Common.Article>(result) }, 201);
        }
    }
}
