using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetTaxCategoriesQueryHandler(ITaxService taxService, IMapper mapper)
        : IRequestHandler<GetTaxCategoriesQueryRequest, ApiResponse<GetTaxCategoriesQueryResponse>>
    {
        public async Task<ApiResponse<GetTaxCategoriesQueryResponse>> Handle(GetTaxCategoriesQueryRequest request, CancellationToken cancellationToken)
        {
            var categories = await taxService.GetTaxCategoriesAsync(cancellationToken);
            var response = new GetTaxCategoriesQueryResponse { TaxCategories = mapper.MapList<Responses.Common.TaxCategory>(categories) };
            return ApiResponse<GetTaxCategoriesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
