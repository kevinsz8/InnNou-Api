using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetActiveUnitTypeCommandHandler(IUnitTypeService unitTypeService, IMapper mapper)
        : IRequestHandler<SetActiveUnitTypeCommandRequest, ApiResponse<SetActiveUnitTypeCommandResponse>>
    {
        public async Task<ApiResponse<SetActiveUnitTypeCommandResponse>> Handle(SetActiveUnitTypeCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await unitTypeService.SetActiveAsync(request.UnitTypeToken, request.IsActive, cancellationToken);
            if (result is null)
                return ApiResponse<SetActiveUnitTypeCommandResponse>.FailureResponse("UNIT_TYPE_NOT_FOUND", "Unit type not found.", 404);

            var response = new SetActiveUnitTypeCommandResponse { UnitType = mapper.Map<Responses.Common.UnitType>(result) };
            return ApiResponse<SetActiveUnitTypeCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
