using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetFamilyTaxCategoryOverridesQueryResponse
    {
        public List<FamilyTaxCategoryOverride> Overrides { get; set; } = [];
    }
}
