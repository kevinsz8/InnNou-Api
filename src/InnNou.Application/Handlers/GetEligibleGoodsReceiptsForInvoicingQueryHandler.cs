using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetEligibleGoodsReceiptsForInvoicingQueryHandler(ISupplierInvoiceService supplierInvoiceService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetEligibleGoodsReceiptsForInvoicingQueryRequest, ApiResponse<GetEligibleGoodsReceiptsForInvoicingQueryResponse>>
    {
        public async Task<ApiResponse<GetEligibleGoodsReceiptsForInvoicingQueryResponse>> Handle(GetEligibleGoodsReceiptsForInvoicingQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await supplierInvoiceService.GetEligibleGoodsReceiptsForInvoicingAsync(
                request.OrganizationToken, request.SupplierToken, request.PurchaseOrderNumber, request.DeliveryNoteNumber,
                request.FromDate, request.ToDate, request.DateType, request.PageNumber, request.PageSize, context, cancellationToken);

            var response = new GetEligibleGoodsReceiptsForInvoicingQueryResponse
            {
                GoodsReceipts = mapper.MapList<Responses.Common.GoodsReceiptForInvoicing>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                HasNextPage = result.PageNumber < result.TotalPages,
                HasPreviousPage = result.PageNumber > 1
            };
            return ApiResponse<GetEligibleGoodsReceiptsForInvoicingQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
