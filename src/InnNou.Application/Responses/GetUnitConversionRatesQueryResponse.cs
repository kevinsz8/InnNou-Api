using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetUnitConversionRatesQueryResponse
    {
        public List<UnitConversionRate> UnitConversionRates { get; set; } = [];
    }
}
