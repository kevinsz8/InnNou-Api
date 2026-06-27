using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditUnitConversionRateCommandHandler(IUnitConversionRateService unitConversionRateService, IMapper mapper)
        : IRequestHandler<EditUnitConversionRateCommandRequest, ApiResponse<EditUnitConversionRateCommandResponse>>
    {
        public async Task<ApiResponse<EditUnitConversionRateCommandResponse>> Handle(EditUnitConversionRateCommandRequest request, CancellationToken cancellationToken)
        {
            var dto = new UnitConversionRateDto { UnitConversionRateToken = request.UnitConversionRateToken, Factor = request.Factor };
            var result = await unitConversionRateService.EditAsync(dto, cancellationToken);
            if (result is null)
                return ApiResponse<EditUnitConversionRateCommandResponse>.FailureResponse("UNIT_CONVERSION_RATE_NOT_FOUND", "Unit conversion rate not found.", 404);

            var response = new EditUnitConversionRateCommandResponse { UnitConversionRate = mapper.Map<Responses.Common.UnitConversionRate>(result) };
            return ApiResponse<EditUnitConversionRateCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
