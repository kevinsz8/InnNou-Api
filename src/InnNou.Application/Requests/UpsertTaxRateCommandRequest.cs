using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class UpsertTaxRateCommandRequest : IRequest<ApiResponse<UpsertTaxRateCommandResponse>>
    {
        public Guid TaxJurisdictionToken { get; set; }
        public Guid TaxCategoryToken { get; set; }
        public decimal RatePercent { get; set; }
    }
}
