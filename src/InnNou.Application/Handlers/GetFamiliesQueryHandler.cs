using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetFamiliesQueryHandler(IFamilyService familyService, IMapper mapper)
        : IRequestHandler<GetFamiliesQueryRequest, ApiResponse<GetFamiliesQueryResponse>>
    {
        public async Task<ApiResponse<GetFamiliesQueryResponse>> Handle(GetFamiliesQueryRequest request, CancellationToken cancellationToken)
        {
            var items = await familyService.GetAllAsync(cancellationToken);
            var response = new GetFamiliesQueryResponse
            {
                Families = mapper.MapList<Responses.Common.Family>(items)
            };
            return ApiResponse<GetFamiliesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
