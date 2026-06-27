using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetUnitsOfMeasureQueryHandler(IUnitOfMeasureService unitOfMeasureService, IUnitTypeService unitTypeService, IMapper mapper)
        : IRequestHandler<GetUnitsOfMeasureQueryRequest, ApiResponse<GetUnitsOfMeasureQueryResponse>>
    {
        public async Task<ApiResponse<GetUnitsOfMeasureQueryResponse>> Handle(GetUnitsOfMeasureQueryRequest request, CancellationToken cancellationToken)
        {
            int? unitTypeId = null;
            if (request.UnitTypeToken.HasValue)
            {
                var unitType = await unitTypeService.GetByTokenAsync(request.UnitTypeToken.Value, cancellationToken);
                if (unitType is null)
                    return ApiResponse<GetUnitsOfMeasureQueryResponse>.FailureResponse("UNIT_TYPE_NOT_FOUND", "Unit type not found.", 404);
                unitTypeId = unitType.UnitTypeId;
            }

            var items = await unitOfMeasureService.GetAllAsync(unitTypeId, cancellationToken);
            var response = new GetUnitsOfMeasureQueryResponse
            {
                UnitsOfMeasure = mapper.MapList<Responses.Common.UnitOfMeasure>(items)
            };
            return ApiResponse<GetUnitsOfMeasureQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
