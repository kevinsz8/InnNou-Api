using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetUnitConversionRatesQueryHandler(IUnitConversionRateService unitConversionRateService, IUnitTypeService unitTypeService, IMapper mapper)
        : IRequestHandler<GetUnitConversionRatesQueryRequest, ApiResponse<GetUnitConversionRatesQueryResponse>>
    {
        public async Task<ApiResponse<GetUnitConversionRatesQueryResponse>> Handle(GetUnitConversionRatesQueryRequest request, CancellationToken cancellationToken)
        {
            int? unitTypeId = null;
            if (request.UnitTypeToken.HasValue)
            {
                var unitType = await unitTypeService.GetByTokenAsync(request.UnitTypeToken.Value, cancellationToken);
                if (unitType is null)
                    return ApiResponse<GetUnitConversionRatesQueryResponse>.FailureResponse("UNIT_TYPE_NOT_FOUND", "Unit type not found.", 404);
                unitTypeId = unitType.UnitTypeId;
            }

            var items = await unitConversionRateService.GetAllAsync(unitTypeId, cancellationToken);
            var response = new GetUnitConversionRatesQueryResponse
            {
                UnitConversionRates = mapper.MapList<Responses.Common.UnitConversionRate>(items)
            };
            return ApiResponse<GetUnitConversionRatesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
