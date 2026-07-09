using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetUnitOfMeasureByTokenQueryHandler(IUnitOfMeasureService unitOfMeasureService, IMapper mapper)
        : IRequestHandler<GetUnitOfMeasureByTokenQueryRequest, ApiResponse<GetUnitOfMeasureByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetUnitOfMeasureByTokenQueryResponse>> Handle(GetUnitOfMeasureByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var dto = await unitOfMeasureService.GetByTokenAsync(request.UnitOfMeasureToken, cancellationToken);
            if (dto is null)
                return ApiResponse<GetUnitOfMeasureByTokenQueryResponse>.FailureResponse(ErrorCodes.UnitOfMeasureNotFound, "Unit of measure not found.", 404);

            var response = new GetUnitOfMeasureByTokenQueryResponse { UnitOfMeasure = mapper.Map<Responses.Common.UnitOfMeasure>(dto) };
            return ApiResponse<GetUnitOfMeasureByTokenQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
