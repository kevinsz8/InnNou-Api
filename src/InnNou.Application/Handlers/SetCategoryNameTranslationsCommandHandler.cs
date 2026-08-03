using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetCategoryNameTranslationsCommandHandler(ICategoryService categoryService, IMapper mapper, IRequestContext context)
        : IRequestHandler<SetCategoryNameTranslationsCommandRequest, ApiResponse<SetCategoryNameTranslationsCommandResponse>>
    {
        // Same supported-language set as InnNou.Shared.Localization's own
        // BulkExcelLocalization/OrderConfirmationLocalization/OrderApprovalEmailLocalization.
        private static readonly HashSet<string> SupportedLanguages = ["en", "es", "ca"];

        public async Task<ApiResponse<SetCategoryNameTranslationsCommandResponse>> Handle(SetCategoryNameTranslationsCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.CategoryToken == Guid.Empty)
                return ApiResponse<SetCategoryNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "CategoryToken is required.", 400);

            if (request.NameTranslations.Keys.Any(k => !SupportedLanguages.Contains(k)) ||
                request.NameTranslations.Values.Any(string.IsNullOrWhiteSpace))
                return ApiResponse<SetCategoryNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "NameTranslations keys must be one of en/es/ca, with non-empty values.", 400);

            var result = await categoryService.SetNameTranslationsAsync(request.CategoryToken, request.NameTranslations, context, cancellationToken);
            if (result is null)
                return ApiResponse<SetCategoryNameTranslationsCommandResponse>.FailureResponse(ErrorCodes.CategoryNotFound, "Category not found.", 404);

            var response = new SetCategoryNameTranslationsCommandResponse { Category = mapper.Map<Responses.Common.Category>(result) };
            return ApiResponse<SetCategoryNameTranslationsCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
