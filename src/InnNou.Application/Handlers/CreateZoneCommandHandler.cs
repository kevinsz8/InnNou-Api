using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateZoneCommandHandler(IZoneService zoneService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateZoneCommandRequest, ApiResponse<CreateZoneCommandResponse>>
    {
        public async Task<ApiResponse<CreateZoneCommandResponse>> Handle(CreateZoneCommandRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.CountryCode) || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
                return ApiResponse<CreateZoneCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "CountryCode, Code and Name are required.", 400);

            var dto = new ZoneDto { Code = request.Code.Trim(), Name = request.Name.Trim() };
            var result = await zoneService.CreateAsync(dto, request.CountryCode.Trim(), context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateZoneCommandResponse>.FailureResponse(ErrorCodes.ZoneCreateFailed, "Zone could not be created.", 500);

            var response = new CreateZoneCommandResponse { Zone = mapper.Map<Responses.Common.Zone>(result) };
            return ApiResponse<CreateZoneCommandResponse>.SuccessResponse(response, 201);
        }
    }
}
