using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetBelowParQueryHandler(IParLevelService parLevelService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetBelowParQueryRequest, ApiResponse<GetBelowParQueryResponse>>
    {
        public async Task<ApiResponse<GetBelowParQueryResponse>> Handle(GetBelowParQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await parLevelService.GetBelowParAsync(request.WarehouseToken, request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetBelowParQueryResponse
            {
                Items = mapper.MapList<Responses.Common.BelowParRow>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetBelowParQueryResponse>.SuccessResponse(response);
        }
    }
}
