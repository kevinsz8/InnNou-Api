using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSubFamilyByTokenQueryHandler(ISubFamilyService subFamilyService, IMapper mapper)
        : IRequestHandler<GetSubFamilyByTokenQueryRequest, ApiResponse<GetSubFamilyByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetSubFamilyByTokenQueryResponse>> Handle(GetSubFamilyByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var dto = await subFamilyService.GetByTokenAsync(request.SubFamilyToken, cancellationToken);
            if (dto is null)
                return ApiResponse<GetSubFamilyByTokenQueryResponse>.FailureResponse("SUB_FAMILY_NOT_FOUND", "Sub-family not found.", 404);

            var response = new GetSubFamilyByTokenQueryResponse { SubFamily = mapper.Map<Responses.Common.SubFamily>(dto) };
            return ApiResponse<GetSubFamilyByTokenQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
