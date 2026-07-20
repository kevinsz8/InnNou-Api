using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetZoneByTokenQueryHandler(IZoneService zoneService, IMapper mapper)
        : IRequestHandler<GetZoneByTokenQueryRequest, ApiResponse<GetZoneByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetZoneByTokenQueryResponse>> Handle(GetZoneByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var zone = await zoneService.GetByTokenAsync(request.ZoneToken, cancellationToken);
            if (zone is null)
                return ApiResponse<GetZoneByTokenQueryResponse>.FailureResponse(ErrorCodes.ZoneNotFound, "Zone not found.", 404);

            var response = new GetZoneByTokenQueryResponse { Zone = mapper.Map<Responses.Common.Zone>(zone) };
            return ApiResponse<GetZoneByTokenQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
