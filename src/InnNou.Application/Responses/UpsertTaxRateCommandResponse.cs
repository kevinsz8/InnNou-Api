using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class UpsertTaxRateCommandResponse
    {
        public TaxRateGridRow Row { get; set; } = default!;
    }
}
