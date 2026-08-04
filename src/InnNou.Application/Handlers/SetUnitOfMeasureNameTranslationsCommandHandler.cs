using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetUnitOfMeasureNameTranslationsCommandHandler(IUnitOfMeasureService unitOfMeasureService, IMapper mapper, IRequestContext context)
        : IRequestHandler<SetUnitOfMeasureNameTranslationsCommandRequest, ApiResponse<SetUnitOfMeasureNameTranslationsCommandResponse>>
    {
        // Same supported-language set as InnNou.Shared.Localization's own
        // BulkExcelLocalization/OrderConfirmationLocalization/OrderApprovalEmailLocalization.
        private static readonly HashSet<string> SupportedLanguages = ["en", "es", "ca"];

        public async Task<ApiResponse<SetUnitOfMeasureNameTranslationsCommandResponse>> Handle(SetUnitOfMeasureNameTranslationsCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.UnitOfMeasureToken == Guid.Empty)
                return ApiResponse<SetUnitOfMeasureNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "UnitOfMeasureToken is required.", 400);

            if (request.NameTranslations.Keys.Any(k => !SupportedLanguages.Contains(k)) ||
                request.NameTranslations.Values.Any(string.IsNullOrWhiteSpace))
                return ApiResponse<SetUnitOfMeasureNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "NameTranslations keys must be one of en/es/ca, with non-empty values.", 400);

            var result = await unitOfMeasureService.SetNameTranslationsAsync(request.UnitOfMeasureToken, request.NameTranslations, context, cancellationToken);
            if (result is null)
                return ApiResponse<SetUnitOfMeasureNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.UnitOfMeasureNotFound, "Unit of measure not found.", 404);

            var response = new SetUnitOfMeasureNameTranslationsCommandResponse { UnitOfMeasure = mapper.Map<Responses.Common.UnitOfMeasure>(result) };
            return ApiResponse<SetUnitOfMeasureNameTranslationsCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
