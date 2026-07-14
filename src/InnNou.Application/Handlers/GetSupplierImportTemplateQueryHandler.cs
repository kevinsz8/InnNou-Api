using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSupplierImportTemplateQueryHandler : IRequestHandler<GetSupplierImportTemplateQueryRequest, FileResult>
    {
        private readonly ISupplierService _supplierService;
        private readonly IRequestContext _context;

        public GetSupplierImportTemplateQueryHandler(ISupplierService supplierService, IRequestContext requestContext)
        {
            _supplierService = supplierService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(GetSupplierImportTemplateQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _supplierService.GenerateSupplierImportTemplateAsync(request.Language, _context, cancellationToken);
            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
