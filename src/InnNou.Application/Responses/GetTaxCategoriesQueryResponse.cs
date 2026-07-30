using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetTaxCategoriesQueryResponse
    {
        public List<TaxCategory> TaxCategories { get; set; } = [];
    }
}
