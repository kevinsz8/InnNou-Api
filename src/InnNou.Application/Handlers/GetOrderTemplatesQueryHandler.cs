using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetOrderTemplatesQueryHandler(IOrderTemplateService orderTemplateService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetOrderTemplatesQueryRequest, ApiResponse<GetOrderTemplatesQueryResponse>>
    {
        public async Task<ApiResponse<GetOrderTemplatesQueryResponse>> Handle(GetOrderTemplatesQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await orderTemplateService.GetPagedAsync(
                request.OrganizationToken, request.WarehouseToken, request.SearchText, request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetOrderTemplatesQueryResponse
            {
                OrderTemplates = mapper.MapList<Responses.Common.OrderTemplate>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetOrderTemplatesQueryResponse>.SuccessResponse(response);
        }
    }
}
