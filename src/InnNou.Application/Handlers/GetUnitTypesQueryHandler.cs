using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetUnitTypesQueryHandler(IUnitTypeService unitTypeService, IMapper mapper)
        : IRequestHandler<GetUnitTypesQueryRequest, ApiResponse<GetUnitTypesQueryResponse>>
    {
        public async Task<ApiResponse<GetUnitTypesQueryResponse>> Handle(GetUnitTypesQueryRequest request, CancellationToken cancellationToken)
        {
            var items = await unitTypeService.GetAllAsync(cancellationToken);
            var response = new GetUnitTypesQueryResponse
            {
                UnitTypes = mapper.MapList<Responses.Common.UnitType>(items)
            };
            return ApiResponse<GetUnitTypesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
