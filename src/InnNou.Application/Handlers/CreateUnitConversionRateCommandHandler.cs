using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateUnitConversionRateCommandHandler(IUnitConversionRateService unitConversionRateService, IUnitOfMeasureService unitOfMeasureService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateUnitConversionRateCommandRequest, ApiResponse<CreateUnitConversionRateCommandResponse>>
    {
        public async Task<ApiResponse<CreateUnitConversionRateCommandResponse>> Handle(CreateUnitConversionRateCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.Factor <= 0)
                return ApiResponse<CreateUnitConversionRateCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Factor must be greater than zero.", 400);

            var fromUnit = await unitOfMeasureService.GetByTokenAsync(request.FromUnitOfMeasureToken, cancellationToken);
            if (fromUnit is null)
                return ApiResponse<CreateUnitConversionRateCommandResponse>.FailureResponse(ErrorCodes.UnitOfMeasureNotFound, "Source unit of measure not found.", 404);

            var toUnit = await unitOfMeasureService.GetByTokenAsync(request.ToUnitOfMeasureToken, cancellationToken);
            if (toUnit is null)
                return ApiResponse<CreateUnitConversionRateCommandResponse>.FailureResponse(ErrorCodes.UnitOfMeasureNotFound, "Target unit of measure not found.", 404);

            var dto = new UnitConversionRateDto
            {
                FromUnitOfMeasureId = fromUnit.UnitOfMeasureId,
                ToUnitOfMeasureId = toUnit.UnitOfMeasureId,
                Factor = request.Factor
            };
            var result = await unitConversionRateService.CreateAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateUnitConversionRateCommandResponse>.FailureResponse(ErrorCodes.UnitConversionRateCreateFailed, "Unit conversion rate could not be created.", 500);

            var response = new CreateUnitConversionRateCommandResponse { UnitConversionRate = mapper.Map<Responses.Common.UnitConversionRate>(result) };
            return ApiResponse<CreateUnitConversionRateCommandResponse>.SuccessResponse(response, 201);
        }
    }
}
