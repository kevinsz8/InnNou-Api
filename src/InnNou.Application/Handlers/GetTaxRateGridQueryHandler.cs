using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetTaxRateGridQueryHandler(ITaxService taxService, IMapper mapper)
        : IRequestHandler<GetTaxRateGridQueryRequest, ApiResponse<GetTaxRateGridQueryResponse>>
    {
        public async Task<ApiResponse<GetTaxRateGridQueryResponse>> Handle(GetTaxRateGridQueryRequest request, CancellationToken cancellationToken)
        {
            var rows = await taxService.GetTaxRateGridAsync(cancellationToken);
            var response = new GetTaxRateGridQueryResponse { Rows = mapper.MapList<Responses.Common.TaxRateGridRow>(rows) };
            return ApiResponse<GetTaxRateGridQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
