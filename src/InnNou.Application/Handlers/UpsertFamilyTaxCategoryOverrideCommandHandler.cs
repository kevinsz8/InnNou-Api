using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class UpsertFamilyTaxCategoryOverrideCommandHandler(ITaxService taxService, IMapper mapper, IRequestContext context)
        : IRequestHandler<UpsertFamilyTaxCategoryOverrideCommandRequest, ApiResponse<UpsertFamilyTaxCategoryOverrideCommandResponse>>
    {
        public async Task<ApiResponse<UpsertFamilyTaxCategoryOverrideCommandResponse>> Handle(UpsertFamilyTaxCategoryOverrideCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.FamilyToken == Guid.Empty || request.TaxJurisdictionToken == Guid.Empty || request.TaxCategoryToken == Guid.Empty)
                return ApiResponse<UpsertFamilyTaxCategoryOverrideCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "FamilyToken, TaxJurisdictionToken and TaxCategoryToken are required.", 400);

            var result = await taxService.UpsertFamilyTaxCategoryOverrideAsync(
                request.FamilyToken, request.TaxJurisdictionToken, request.TaxCategoryToken, context, cancellationToken);

            var response = new UpsertFamilyTaxCategoryOverrideCommandResponse { Override = mapper.Map<Responses.Common.FamilyTaxCategoryOverride>(result) };
            return ApiResponse<UpsertFamilyTaxCategoryOverrideCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
