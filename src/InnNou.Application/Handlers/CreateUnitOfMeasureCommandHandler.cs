using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateUnitOfMeasureCommandHandler(IUnitOfMeasureService unitOfMeasureService, IUnitTypeService unitTypeService, IMapper mapper)
        : IRequestHandler<CreateUnitOfMeasureCommandRequest, ApiResponse<CreateUnitOfMeasureCommandResponse>>
    {
        public async Task<ApiResponse<CreateUnitOfMeasureCommandResponse>> Handle(CreateUnitOfMeasureCommandRequest request, CancellationToken cancellationToken)
        {
            var unitType = await unitTypeService.GetByTokenAsync(request.UnitTypeToken, cancellationToken);
            if (unitType is null)
                return ApiResponse<CreateUnitOfMeasureCommandResponse>.FailureResponse(ErrorCodes.UnitTypeNotFound, "Unit type not found.", 404);

            if (await unitOfMeasureService.ExistsByCodeAsync(request.Code, unitType.UnitTypeId, cancellationToken))
                return ApiResponse<CreateUnitOfMeasureCommandResponse>.FailureResponse(ErrorCodes.UnitOfMeasureCodeExists, "A unit of measure with this code already exists in the unit type.", 409);

            var dto = new UnitOfMeasureDto
            {
                UnitTypeId = unitType.UnitTypeId,
                Code = request.Code,
                Symbol = request.Symbol,
                Decimals = request.Decimals
            };
            var result = await unitOfMeasureService.CreateAsync(dto, cancellationToken);
            if (result is null)
                return ApiResponse<CreateUnitOfMeasureCommandResponse>.FailureResponse(ErrorCodes.UnitOfMeasureCreateFailed, "Unit of measure could not be created.", 500);

            var response = new CreateUnitOfMeasureCommandResponse { UnitOfMeasure = mapper.Map<Responses.Common.UnitOfMeasure>(result) };
            return ApiResponse<CreateUnitOfMeasureCommandResponse>.SuccessResponse(response, 201);
        }
    }
}
