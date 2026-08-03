using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetFamilyTaxCategoryOverridesQueryHandler(ITaxService taxService, IMapper mapper)
        : IRequestHandler<GetFamilyTaxCategoryOverridesQueryRequest, ApiResponse<GetFamilyTaxCategoryOverridesQueryResponse>>
    {
        public async Task<ApiResponse<GetFamilyTaxCategoryOverridesQueryResponse>> Handle(GetFamilyTaxCategoryOverridesQueryRequest request, CancellationToken cancellationToken)
        {
            var overrides = await taxService.GetFamilyTaxCategoryOverridesAsync(request.FamilyToken, cancellationToken);
            var response = new GetFamilyTaxCategoryOverridesQueryResponse { Overrides = mapper.MapList<Responses.Common.FamilyTaxCategoryOverride>(overrides) };
            return ApiResponse<GetFamilyTaxCategoryOverridesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
