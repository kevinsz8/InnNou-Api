using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetTaxJurisdictionsQueryHandler(ITaxService taxService, IMapper mapper)
        : IRequestHandler<GetTaxJurisdictionsQueryRequest, ApiResponse<GetTaxJurisdictionsQueryResponse>>
    {
        public async Task<ApiResponse<GetTaxJurisdictionsQueryResponse>> Handle(GetTaxJurisdictionsQueryRequest request, CancellationToken cancellationToken)
        {
            var jurisdictions = await taxService.GetTaxJurisdictionsAsync(cancellationToken);
            var response = new GetTaxJurisdictionsQueryResponse { TaxJurisdictions = mapper.MapList<Responses.Common.TaxJurisdiction>(jurisdictions) };
            return ApiResponse<GetTaxJurisdictionsQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
