using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSupplierInvoicesQueryHandler(ISupplierInvoiceService supplierInvoiceService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetSupplierInvoicesQueryRequest, ApiResponse<GetSupplierInvoicesQueryResponse>>
    {
        public async Task<ApiResponse<GetSupplierInvoicesQueryResponse>> Handle(GetSupplierInvoicesQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await supplierInvoiceService.GetPagedAsync(
                request.OrganizationToken, request.SupplierToken, request.Status, request.SearchText,
                request.FromDate, request.ToDate, request.PageNumber, request.PageSize, context, cancellationToken);

            var response = new GetSupplierInvoicesQueryResponse
            {
                SupplierInvoices = mapper.MapList<Responses.Common.SupplierInvoice>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                HasNextPage = result.PageNumber < result.TotalPages,
                HasPreviousPage = result.PageNumber > 1
            };
            return ApiResponse<GetSupplierInvoicesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
