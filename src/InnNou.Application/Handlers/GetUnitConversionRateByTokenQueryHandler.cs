using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetUnitConversionRateByTokenQueryHandler(IUnitConversionRateService unitConversionRateService, IMapper mapper)
        : IRequestHandler<GetUnitConversionRateByTokenQueryRequest, ApiResponse<GetUnitConversionRateByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetUnitConversionRateByTokenQueryResponse>> Handle(GetUnitConversionRateByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var dto = await unitConversionRateService.GetByTokenAsync(request.UnitConversionRateToken, cancellationToken);
            if (dto is null)
                return ApiResponse<GetUnitConversionRateByTokenQueryResponse>.FailureResponse("UNIT_CONVERSION_RATE_NOT_FOUND", "Unit conversion rate not found.", 404);

            var response = new GetUnitConversionRateByTokenQueryResponse { UnitConversionRate = mapper.Map<Responses.Common.UnitConversionRate>(dto) };
            return ApiResponse<GetUnitConversionRateByTokenQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
