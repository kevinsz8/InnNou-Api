using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSupplierReturnsQueryHandler(ISupplierReturnService supplierReturnService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetSupplierReturnsQueryRequest, ApiResponse<GetSupplierReturnsQueryResponse>>
    {
        public async Task<ApiResponse<GetSupplierReturnsQueryResponse>> Handle(GetSupplierReturnsQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await supplierReturnService.GetPagedAsync(
                request.OrganizationToken, request.SupplierToken, request.Status, request.FromDate, request.ToDate,
                request.PurchaseOrderNumber, request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetSupplierReturnsQueryResponse
            {
                SupplierReturns = mapper.MapList<Responses.Common.SupplierReturn>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetSupplierReturnsQueryResponse>.SuccessResponse(response);
        }
    }
}
