using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetSubCategoryNameTranslationsCommandHandler(ISubCategoryService subCategoryService, IMapper mapper, IRequestContext context)
        : IRequestHandler<SetSubCategoryNameTranslationsCommandRequest, ApiResponse<SetSubCategoryNameTranslationsCommandResponse>>
    {
        // Same supported-language set as InnNou.Shared.Localization's own
        // BulkExcelLocalization/OrderConfirmationLocalization/OrderApprovalEmailLocalization.
        private static readonly HashSet<string> SupportedLanguages = ["en", "es", "ca"];

        public async Task<ApiResponse<SetSubCategoryNameTranslationsCommandResponse>> Handle(SetSubCategoryNameTranslationsCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.SubCategoryToken == Guid.Empty)
                return ApiResponse<SetSubCategoryNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "SubCategoryToken is required.", 400);

            if (request.NameTranslations.Keys.Any(k => !SupportedLanguages.Contains(k)) ||
                request.NameTranslations.Values.Any(string.IsNullOrWhiteSpace))
                return ApiResponse<SetSubCategoryNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "NameTranslations keys must be one of en/es/ca, with non-empty values.", 400);

            var result = await subCategoryService.SetNameTranslationsAsync(request.SubCategoryToken, request.NameTranslations, context, cancellationToken);
            if (result is null)
                return ApiResponse<SetSubCategoryNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.SubCategoryNotFound, "Sub-category not found.", 404);

            var response = new SetSubCategoryNameTranslationsCommandResponse { SubCategory = mapper.Map<Responses.Common.SubCategory>(result) };
            return ApiResponse<SetSubCategoryNameTranslationsCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
