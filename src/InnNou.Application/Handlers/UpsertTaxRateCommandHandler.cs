using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class UpsertTaxRateCommandHandler(ITaxService taxService, IMapper mapper, IRequestContext context)
        : IRequestHandler<UpsertTaxRateCommandRequest, ApiResponse<UpsertTaxRateCommandResponse>>
    {
        public async Task<ApiResponse<UpsertTaxRateCommandResponse>> Handle(UpsertTaxRateCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.TaxJurisdictionToken == Guid.Empty || request.TaxCategoryToken == Guid.Empty)
                return ApiResponse<UpsertTaxRateCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "TaxJurisdictionToken and TaxCategoryToken are required.", 400);

            if (request.RatePercent < 0 || request.RatePercent > 100)
                return ApiResponse<UpsertTaxRateCommandResponse>.FailureResponse(ErrorCodes.TaxRateInvalidPercent, "A tax rate must be between 0 and 100.", 400);

            var result = await taxService.UpsertTaxRateAsync(request.TaxJurisdictionToken, request.TaxCategoryToken, request.RatePercent, context, cancellationToken);
            if (result is null)
                return ApiResponse<UpsertTaxRateCommandResponse>.FailureResponse(ErrorCodes.TaxJurisdictionNotFound, "Tax rate not found after upsert.", 404);

            var response = new UpsertTaxRateCommandResponse { Row = mapper.Map<Responses.Common.TaxRateGridRow>(result) };
            return ApiResponse<UpsertTaxRateCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
