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
                return ApiResponse<CreateArticleCommandResponse>.FailureResponse("SUPPLIER_NOT_FOUND", "Supplier not found.", 404);

            var purchaseUnit = await unitOfMeasureService.GetByTokenAsync(request.PurchaseUnitToken, cancellationToken);
            if (purchaseUnit is null)
                return ApiResponse<CreateArticleCommandResponse>.FailureResponse("PURCHASE_UNIT_NOT_FOUND", "Purchase unit of measure not found.", 404);

            var contentUnit = await unitOfMeasureService.GetByTokenAsync(request.ContentUnitToken, cancellationToken);
            if (contentUnit is null)
                return ApiResponse<CreateArticleCommandResponse>.FailureResponse("CONTENT_UNIT_NOT_FOUND", "Content unit of measure not found.", 404);

            int? baseUnitId = null;
            if (request.BaseUnitToken.HasValue)
            {
                var baseUnit = await unitOfMeasureService.GetByTokenAsync(request.BaseUnitToken.Value, cancellationToken);
                if (baseUnit is null)
                    return ApiResponse<CreateArticleCommandResponse>.FailureResponse("BASE_UNIT_NOT_FOUND", "Base unit of measure not found.", 404);
                baseUnitId = baseUnit.UnitOfMeasureId;
            }

            int? familyId = null;
            if (request.FamilyToken.HasValue)
            {
                var family = await familyService.GetByTokenAsync(request.FamilyToken.Value, cancellationToken);
                if (family is null)
                    return ApiResponse<CreateArticleCommandResponse>.FailureResponse("FAMILY_NOT_FOUND", "Family not found.", 404);
                familyId = family.FamilyId;
            }

            int? subFamilyId = null;
            if (request.SubFamilyToken.HasValue)
            {
                var subFamily = await subFamilyService.GetByTokenAsync(request.SubFamilyToken.Value, cancellationToken);
                if (subFamily is null)
                    return ApiResponse<CreateArticleCommandResponse>.FailureResponse("SUBFAMILY_NOT_FOUND", "Sub-family not found.", 404);
                subFamilyId = subFamily.SubFamilyId;
            }

            if (!string.IsNullOrWhiteSpace(request.SupplierSku))
            {
                var skuExists = await articleService.ExistsBySupplierSkuAsync(supplier.SupplierId, request.SupplierSku, null, cancellationToken);
                if (skuExists)
                    return ApiResponse<CreateArticleCommandResponse>.FailureResponse("ARTICLE_SKU_EXISTS", "An article with this SKU already exists for this supplier.", 409);
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
                PurchaseQuantity = request.PurchaseQuantity,
                ContentUnitId = contentUnit.UnitOfMeasureId,
                ContentQuantity = request.ContentQuantity,
                BaseUnitId = baseUnitId,
                MinimumOrderQty = request.MinimumOrderQty,
                LeadTimeDays = request.LeadTimeDays
            };

            var result = await articleService.CreateAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateArticleCommandResponse>.FailureResponse("ARTICLE_CREATE_FAILED", "Article could not be created.", 500);

            return ApiResponse<CreateArticleCommandResponse>.SuccessResponse(
                new CreateArticleCommandResponse { Article = mapper.Map<Responses.Common.Article>(result) }, 201);
        }
    }
}
