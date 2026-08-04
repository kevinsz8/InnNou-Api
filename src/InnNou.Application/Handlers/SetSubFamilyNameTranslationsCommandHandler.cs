using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetSubFamilyNameTranslationsCommandHandler(ISubFamilyService subFamilyService, IMapper mapper, IRequestContext context)
        : IRequestHandler<SetSubFamilyNameTranslationsCommandRequest, ApiResponse<SetSubFamilyNameTranslationsCommandResponse>>
    {
        // Same supported-language set as InnNou.Shared.Localization's own
        // BulkExcelLocalization/OrderConfirmationLocalization/OrderApprovalEmailLocalization.
        private static readonly HashSet<string> SupportedLanguages = ["en", "es", "ca"];

        public async Task<ApiResponse<SetSubFamilyNameTranslationsCommandResponse>> Handle(SetSubFamilyNameTranslationsCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.SubFamilyToken == Guid.Empty)
                return ApiResponse<SetSubFamilyNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "SubFamilyToken is required.", 400);

            if (request.NameTranslations.Keys.Any(k => !SupportedLanguages.Contains(k)) ||
                request.NameTranslations.Values.Any(string.IsNullOrWhiteSpace))
                return ApiResponse<SetSubFamilyNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "NameTranslations keys must be one of en/es/ca, with non-empty values.", 400);

            var result = await subFamilyService.SetNameTranslationsAsync(request.SubFamilyToken, request.NameTranslations, context, cancellationToken);
            if (result is null)
                return ApiResponse<SetSubFamilyNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.SubFamilyNotFound, "Sub-family not found.", 404);

            var response = new SetSubFamilyNameTranslationsCommandResponse { SubFamily = mapper.Map<Responses.Common.SubFamily>(result) };
            return ApiResponse<SetSubFamilyNameTranslationsCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
