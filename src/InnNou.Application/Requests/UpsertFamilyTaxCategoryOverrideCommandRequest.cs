using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class UpsertFamilyTaxCategoryOverrideCommandRequest : IRequest<ApiResponse<UpsertFamilyTaxCategoryOverrideCommandResponse>>
    {
        public Guid FamilyToken { get; set; }
        public Guid TaxJurisdictionToken { get; set; }
        public Guid TaxCategoryToken { get; set; }
    }
}
