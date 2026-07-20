using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditZoneCommandHandler(IZoneService zoneService, IMapper mapper, IRequestContext context)
        : IRequestHandler<EditZoneCommandRequest, ApiResponse<EditZoneCommandResponse>>
    {
        public async Task<ApiResponse<EditZoneCommandResponse>> Handle(EditZoneCommandRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
                return ApiResponse<EditZoneCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Code and Name are required.", 400);

            var dto = new ZoneDto { ZoneToken = request.ZoneToken, Code = request.Code.Trim(), Name = request.Name.Trim() };
            var result = await zoneService.EditAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<EditZoneCommandResponse>.FailureResponse(ErrorCodes.ZoneNotFound, "Zone not found.", 404);

            var response = new EditZoneCommandResponse { Zone = mapper.Map<Responses.Common.Zone>(result) };
            return ApiResponse<EditZoneCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
