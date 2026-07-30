using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetTaxRateGridQueryResponse
    {
        public List<TaxRateGridRow> Rows { get; set; } = [];
    }
}
