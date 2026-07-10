using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetCurrenciesQueryHandler(ICurrencyService currencyService, IMapper mapper)
        : IRequestHandler<GetCurrenciesQueryRequest, ApiResponse<GetCurrenciesQueryResponse>>
    {
        public async Task<ApiResponse<GetCurrenciesQueryResponse>> Handle(GetCurrenciesQueryRequest request, CancellationToken cancellationToken)
        {
            var currencies = await currencyService.GetAllAsync(request.IncludeInactive, cancellationToken);
            return ApiResponse<GetCurrenciesQueryResponse>.SuccessResponse(
                new GetCurrenciesQueryResponse { Currencies = mapper.MapList<Responses.Common.Currency>(currencies) }, 200);
        }
    }
}
