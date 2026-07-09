using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetFamilyByTokenQueryHandler(IFamilyService familyService, IMapper mapper)
        : IRequestHandler<GetFamilyByTokenQueryRequest, ApiResponse<GetFamilyByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetFamilyByTokenQueryResponse>> Handle(GetFamilyByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var dto = await familyService.GetByTokenAsync(request.FamilyToken, cancellationToken);
            if (dto is null)
                return ApiResponse<GetFamilyByTokenQueryResponse>.FailureResponse(ErrorCodes.FamilyNotFound, "Family not found.", 404);

            var response = new GetFamilyByTokenQueryResponse { Family = mapper.Map<Responses.Common.Family>(dto) };
            return ApiResponse<GetFamilyByTokenQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
