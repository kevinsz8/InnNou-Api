using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetUnitTypeByTokenQueryHandler(IUnitTypeService unitTypeService, IMapper mapper)
        : IRequestHandler<GetUnitTypeByTokenQueryRequest, ApiResponse<GetUnitTypeByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetUnitTypeByTokenQueryResponse>> Handle(GetUnitTypeByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var dto = await unitTypeService.GetByTokenAsync(request.UnitTypeToken, cancellationToken);
            if (dto is null)
                return ApiResponse<GetUnitTypeByTokenQueryResponse>.FailureResponse("UNIT_TYPE_NOT_FOUND", "Unit type not found.", 404);
            var response = new GetUnitTypeByTokenQueryResponse { UnitType = mapper.Map<Responses.Common.UnitType>(dto) };
            return ApiResponse<GetUnitTypeByTokenQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
