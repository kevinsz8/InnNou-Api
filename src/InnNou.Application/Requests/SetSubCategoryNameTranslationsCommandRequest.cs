using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    // Keys must be a subset of the app's own i18next language codes (en/es/ca) — see
    // .claude/CatalogTranslationsModule.md. Empty/omitted dictionary clears all translations.
    public class SetSubCategoryNameTranslationsCommandRequest : IRequest<ApiResponse<SetSubCategoryNameTranslationsCommandResponse>>
    {
        public Guid SubCategoryToken { get; set; }
        public Dictionary<string, string> NameTranslations { get; set; } = [];
    }
}
