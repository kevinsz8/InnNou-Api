using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateSupplierInvoiceCommandHandler(ISupplierInvoiceService supplierInvoiceService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateSupplierInvoiceCommandRequest, ApiResponse<CreateSupplierInvoiceCommandResponse>>
    {
        public async Task<ApiResponse<CreateSupplierInvoiceCommandResponse>> Handle(CreateSupplierInvoiceCommandRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SupplierInvoiceNumber))
                return ApiResponse<CreateSupplierInvoiceCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "SupplierInvoiceNumber is required.", 400);

            if (request.GoodsReceiptTokens is null || request.GoodsReceiptTokens.Count == 0)
                return ApiResponse<CreateSupplierInvoiceCommandResponse>.FailureResponse(ErrorCodes.SupplierInvoiceEmpty, "At least one goods receipt must be selected.", 400);

            if (request.Lines is null || request.Lines.Count == 0)
                return ApiResponse<CreateSupplierInvoiceCommandResponse>.FailureResponse(ErrorCodes.SupplierInvoiceEmpty, "At least one line must be invoiced.", 400);

            if (request.TaxBreakdown is null || request.TaxBreakdown.Count == 0)
                return ApiResponse<CreateSupplierInvoiceCommandResponse>.FailureResponse(ErrorCodes.SupplierInvoiceTaxBreakdownRequired, "At least one tax-rate breakdown row (Base Fra) is required, transcribed from the supplier's real invoice.", 400);

            var lines = request.Lines.Select(l => new CreateSupplierInvoiceLineInputDto
            {
                GoodsReceiptLineToken = l.GoodsReceiptLineToken,
                QuantityInvoiced = l.QuantityInvoiced,
                UnitPriceInvoiced = l.UnitPriceInvoiced
            }).ToList();

            var taxBreakdown = request.TaxBreakdown.Select(b => new CreateSupplierInvoiceTaxBreakdownInputDto
            {
                TaxRatePercent = b.TaxRatePercent,
                BaseAmount = b.BaseAmount
            }).ToList();

            var result = await supplierInvoiceService.CreateAsync(
                request.OrganizationToken, request.SupplierToken, request.SupplierInvoiceNumber.Trim(), request.InvoiceDate, request.Notes,
                request.GoodsReceiptTokens, lines, taxBreakdown, context, cancellationToken);

            if (result is null)
                return ApiResponse<CreateSupplierInvoiceCommandResponse>.FailureResponse(ErrorCodes.SupplierInvoiceNotFound, "Supplier invoice could not be created.", 500);

            var response = new CreateSupplierInvoiceCommandResponse { SupplierInvoice = mapper.Map<Responses.Common.SupplierInvoice>(result) };
            return ApiResponse<CreateSupplierInvoiceCommandResponse>.SuccessResponse(response, 201);
        }
    }
}
