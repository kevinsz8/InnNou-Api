using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ExportOrderTemplateQueryHandler : IRequestHandler<ExportOrderTemplateQueryRequest, FileResult>
    {
        private readonly IOrderTemplateService _orderTemplateService;
        private readonly IRequestContext _context;

        public ExportOrderTemplateQueryHandler(IOrderTemplateService orderTemplateService, IRequestContext requestContext)
        {
            _orderTemplateService = orderTemplateService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(ExportOrderTemplateQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _orderTemplateService.ExportAsync(request.OrderTemplateToken, request.Language, _context, cancellationToken);
            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
