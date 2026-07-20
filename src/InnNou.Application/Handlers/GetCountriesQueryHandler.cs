using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetCountriesQueryHandler(ICountryService countryService, IMapper mapper)
        : IRequestHandler<GetCountriesQueryRequest, ApiResponse<GetCountriesQueryResponse>>
    {
        public async Task<ApiResponse<GetCountriesQueryResponse>> Handle(GetCountriesQueryRequest request, CancellationToken cancellationToken)
        {
            var countries = await countryService.GetAllAsync(request.IncludeInactive, cancellationToken);
            var response = new GetCountriesQueryResponse { Countries = mapper.MapList<Responses.Common.Country>(countries) };
            return ApiResponse<GetCountriesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
