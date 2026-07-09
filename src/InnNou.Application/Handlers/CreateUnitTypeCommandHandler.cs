using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateUnitTypeCommandHandler(IUnitTypeService unitTypeService, IMapper mapper)
        : IRequestHandler<CreateUnitTypeCommandRequest, ApiResponse<CreateUnitTypeCommandResponse>>
    {
        public async Task<ApiResponse<CreateUnitTypeCommandResponse>> Handle(CreateUnitTypeCommandRequest request, CancellationToken cancellationToken)
        {
            if (await unitTypeService.ExistsByCodeAsync(request.Code, cancellationToken))
                return ApiResponse<CreateUnitTypeCommandResponse>.FailureResponse(ErrorCodes.UnitTypeCodeExists, "A unit type with this code already exists.", 409);

            var dto = new UnitTypeDto { Code = request.Code };
            var result = await unitTypeService.CreateAsync(dto, cancellationToken);
            if (result is null)
                return ApiResponse<CreateUnitTypeCommandResponse>.FailureResponse(ErrorCodes.UnitTypeCreateFailed, "Unit type could not be created.", 500);

            var response = new CreateUnitTypeCommandResponse { UnitType = mapper.Map<Responses.Common.UnitType>(result) };
            return ApiResponse<CreateUnitTypeCommandResponse>.SuccessResponse(response, 201);
        }
    }
}
