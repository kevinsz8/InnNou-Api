using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSupplierCreditNotesQueryHandler(ISupplierCreditNoteService supplierCreditNoteService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetSupplierCreditNotesQueryRequest, ApiResponse<GetSupplierCreditNotesQueryResponse>>
    {
        public async Task<ApiResponse<GetSupplierCreditNotesQueryResponse>> Handle(GetSupplierCreditNotesQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await supplierCreditNoteService.GetPagedAsync(
                request.OrganizationToken, request.SupplierToken, request.FromDate, request.ToDate, request.PurchaseOrderNumber,
                request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetSupplierCreditNotesQueryResponse
            {
                SupplierCreditNotes = mapper.MapList<Responses.Common.SupplierCreditNote>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetSupplierCreditNotesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
