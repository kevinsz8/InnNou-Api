using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetActiveZoneCommandHandler(IZoneService zoneService, IMapper mapper, IRequestContext context)
        : IRequestHandler<SetActiveZoneCommandRequest, ApiResponse<SetActiveZoneCommandResponse>>
    {
        public async Task<ApiResponse<SetActiveZoneCommandResponse>> Handle(SetActiveZoneCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await zoneService.SetActiveAsync(request.ZoneToken, request.IsActive, context, cancellationToken);
            if (result is null)
                return ApiResponse<SetActiveZoneCommandResponse>.FailureResponse(ErrorCodes.ZoneNotFound, "Zone not found.", 404);

            var response = new SetActiveZoneCommandResponse { Zone = mapper.Map<Responses.Common.Zone>(result) };
            return ApiResponse<SetActiveZoneCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
