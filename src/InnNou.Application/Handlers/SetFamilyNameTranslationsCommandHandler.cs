using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetFamilyNameTranslationsCommandHandler(IFamilyService familyService, IMapper mapper, IRequestContext context)
        : IRequestHandler<SetFamilyNameTranslationsCommandRequest, ApiResponse<SetFamilyNameTranslationsCommandResponse>>
    {
        // Same supported-language set as InnNou.Shared.Localization's own
        // BulkExcelLocalization/OrderConfirmationLocalization/OrderApprovalEmailLocalization.
        private static readonly HashSet<string> SupportedLanguages = ["en", "es", "ca"];

        public async Task<ApiResponse<SetFamilyNameTranslationsCommandResponse>> Handle(SetFamilyNameTranslationsCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.FamilyToken == Guid.Empty)
                return ApiResponse<SetFamilyNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "FamilyToken is required.", 400);

            if (request.NameTranslations.Keys.Any(k => !SupportedLanguages.Contains(k)) ||
                request.NameTranslations.Values.Any(string.IsNullOrWhiteSpace))
                return ApiResponse<SetFamilyNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "NameTranslations keys must be one of en/es/ca, with non-empty values.", 400);

            var result = await familyService.SetNameTranslationsAsync(request.FamilyToken, request.NameTranslations, context, cancellationToken);
            if (result is null)
                return ApiResponse<SetFamilyNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.FamilyNotFound, "Family not found.", 404);

            var response = new SetFamilyNameTranslationsCommandResponse { Family = mapper.Map<Responses.Common.Family>(result) };
            return ApiResponse<SetFamilyNameTranslationsCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
