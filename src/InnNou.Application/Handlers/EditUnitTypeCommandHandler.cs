using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditUnitTypeCommandHandler(IUnitTypeService unitTypeService, IMapper mapper)
        : IRequestHandler<EditUnitTypeCommandRequest, ApiResponse<EditUnitTypeCommandResponse>>
    {
        public async Task<ApiResponse<EditUnitTypeCommandResponse>> Handle(EditUnitTypeCommandRequest request, CancellationToken cancellationToken)
        {
            var dto = new UnitTypeDto { UnitTypeToken = request.UnitTypeToken, Code = request.Code };
            var result = await unitTypeService.EditAsync(dto, cancellationToken);
            if (result is null)
                return ApiResponse<EditUnitTypeCommandResponse>.FailureResponse("UNIT_TYPE_NOT_FOUND", "Unit type not found.", 404);

            var response = new EditUnitTypeCommandResponse { UnitType = mapper.Map<Responses.Common.UnitType>(result) };
            return ApiResponse<EditUnitTypeCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
