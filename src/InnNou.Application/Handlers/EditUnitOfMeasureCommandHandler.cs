using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditUnitOfMeasureCommandHandler(IUnitOfMeasureService unitOfMeasureService, IMapper mapper)
        : IRequestHandler<EditUnitOfMeasureCommandRequest, ApiResponse<EditUnitOfMeasureCommandResponse>>
    {
        public async Task<ApiResponse<EditUnitOfMeasureCommandResponse>> Handle(EditUnitOfMeasureCommandRequest request, CancellationToken cancellationToken)
        {
            var dto = new UnitOfMeasureDto
            {
                UnitOfMeasureToken = request.UnitOfMeasureToken,
                Code = request.Code,
                Symbol = request.Symbol,
                Decimals = request.Decimals
            };
            var result = await unitOfMeasureService.EditAsync(dto, cancellationToken);
            if (result is null)
                return ApiResponse<EditUnitOfMeasureCommandResponse>.FailureResponse(ErrorCodes.UnitOfMeasureNotFound, "Unit of measure not found.", 404);

            var response = new EditUnitOfMeasureCommandResponse { UnitOfMeasure = mapper.Map<Responses.Common.UnitOfMeasure>(result) };
            return ApiResponse<EditUnitOfMeasureCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
