using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteUnitConversionRateCommandHandler(IUnitConversionRateService unitConversionRateService)
        : IRequestHandler<DeleteUnitConversionRateCommandRequest, ApiResponse<DeleteUnitConversionRateCommandResponse>>
    {
        public async Task<ApiResponse<DeleteUnitConversionRateCommandResponse>> Handle(DeleteUnitConversionRateCommandRequest request, CancellationToken cancellationToken)
        {
            var deleted = await unitConversionRateService.DeleteAsync(request.UnitConversionRateToken, cancellationToken);
            if (!deleted)
                return ApiResponse<DeleteUnitConversionRateCommandResponse>.FailureResponse(ErrorCodes.UnitConversionRateNotFound, "Unit conversion rate not found.", 404);

            return ApiResponse<DeleteUnitConversionRateCommandResponse>.SuccessResponse(
                new DeleteUnitConversionRateCommandResponse { UnitConversionRateToken = request.UnitConversionRateToken, Success = true }, 200);
        }
    }
}
