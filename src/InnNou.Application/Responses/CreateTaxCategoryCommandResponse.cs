using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class CreateTaxCategoryCommandResponse
    {
        public TaxCategory TaxCategory { get; set; } = default!;
    }
}
