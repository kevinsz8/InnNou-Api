using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetActiveUnitOfMeasureCommandHandler(IUnitOfMeasureService unitOfMeasureService, IMapper mapper)
        : IRequestHandler<SetActiveUnitOfMeasureCommandRequest, ApiResponse<SetActiveUnitOfMeasureCommandResponse>>
    {
        public async Task<ApiResponse<SetActiveUnitOfMeasureCommandResponse>> Handle(SetActiveUnitOfMeasureCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await unitOfMeasureService.SetActiveAsync(request.UnitOfMeasureToken, request.IsActive, cancellationToken);
            if (result is null)
                return ApiResponse<SetActiveUnitOfMeasureCommandResponse>.FailureResponse("UNIT_OF_MEASURE_NOT_FOUND", "Unit of measure not found.", 404);

            var response = new SetActiveUnitOfMeasureCommandResponse { UnitOfMeasure = mapper.Map<Responses.Common.UnitOfMeasure>(result) };
            return ApiResponse<SetActiveUnitOfMeasureCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
