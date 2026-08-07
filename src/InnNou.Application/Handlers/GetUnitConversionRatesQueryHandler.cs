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
                    return ApiResponse<GetUnitConversionRatesQueryResponse>.FailureResponse(ErrorCodes.UnitTypeNotFound, "Unit type not found.", 404);
                unitTypeId = unitType.UnitTypeId;
            }

            var result = await unitConversionRateService.GetPagedAsync(request.PageNumber, request.PageSize, unitTypeId, request.IncludeInactive, cancellationToken);
            var totalPages = result.TotalPages;
            var response = new GetUnitConversionRatesQueryResponse
            {
                UnitConversionRates = mapper.MapList<Responses.Common.UnitConversionRate>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetUnitConversionRatesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
