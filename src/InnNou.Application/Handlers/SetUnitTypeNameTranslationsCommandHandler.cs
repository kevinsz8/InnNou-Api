using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetUnitTypeNameTranslationsCommandHandler(IUnitTypeService unitTypeService, IMapper mapper, IRequestContext context)
        : IRequestHandler<SetUnitTypeNameTranslationsCommandRequest, ApiResponse<SetUnitTypeNameTranslationsCommandResponse>>
    {
        // Same supported-language set as InnNou.Shared.Localization's own
        // BulkExcelLocalization/OrderConfirmationLocalization/OrderApprovalEmailLocalization.
        private static readonly HashSet<string> SupportedLanguages = ["en", "es", "ca"];

        public async Task<ApiResponse<SetUnitTypeNameTranslationsCommandResponse>> Handle(SetUnitTypeNameTranslationsCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.UnitTypeToken == Guid.Empty)
                return ApiResponse<SetUnitTypeNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "UnitTypeToken is required.", 400);

            if (request.NameTranslations.Keys.Any(k => !SupportedLanguages.Contains(k)) ||
                request.NameTranslations.Values.Any(string.IsNullOrWhiteSpace))
                return ApiResponse<SetUnitTypeNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "NameTranslations keys must be one of en/es/ca, with non-empty values.", 400);

            var result = await unitTypeService.SetNameTranslationsAsync(request.UnitTypeToken, request.NameTranslations, context, cancellationToken);
            if (result is null)
                return ApiResponse<SetUnitTypeNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.UnitTypeNotFound, "Unit type not found.", 404);

            var response = new SetUnitTypeNameTranslationsCommandResponse { UnitType = mapper.Map<Responses.Common.UnitType>(result) };
            return ApiResponse<SetUnitTypeNameTranslationsCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
