using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ExportSuppliersQueryHandler : IRequestHandler<ExportSuppliersQueryRequest, FileResult>
    {
        private readonly ISupplierService _supplierService;
        private readonly IRequestContext _context;

        public ExportSuppliersQueryHandler(ISupplierService supplierService, IRequestContext requestContext)
        {
            _supplierService = supplierService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(ExportSuppliersQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _supplierService.ExportSuppliersAsync(
                request.SearchField, request.SearchText, request.IncludeInactive, _context, cancellationToken);

            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
