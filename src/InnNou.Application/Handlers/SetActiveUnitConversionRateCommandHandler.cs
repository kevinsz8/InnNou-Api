using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetActiveUnitConversionRateCommandHandler(IUnitConversionRateService unitConversionRateService, IMapper mapper)
        : IRequestHandler<SetActiveUnitConversionRateCommandRequest, ApiResponse<SetActiveUnitConversionRateCommandResponse>>
    {
        public async Task<ApiResponse<SetActiveUnitConversionRateCommandResponse>> Handle(SetActiveUnitConversionRateCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await unitConversionRateService.SetActiveAsync(request.UnitConversionRateToken, request.IsActive, cancellationToken);
            if (result is null)
                return ApiResponse<SetActiveUnitConversionRateCommandResponse>.FailureResponse("UNIT_CONVERSION_RATE_NOT_FOUND", "Unit conversion rate not found.", 404);

            var response = new SetActiveUnitConversionRateCommandResponse { UnitConversionRate = mapper.Map<Responses.Common.UnitConversionRate>(result) };
            return ApiResponse<SetActiveUnitConversionRateCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
