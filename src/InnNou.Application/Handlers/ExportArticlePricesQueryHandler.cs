using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ExportArticlePricesQueryHandler : IRequestHandler<ExportArticlePricesQueryRequest, FileResult>
    {
        private readonly IArticlePriceService _articlePriceService;
        private readonly IRequestContext _context;

        public ExportArticlePricesQueryHandler(IArticlePriceService articlePriceService, IRequestContext requestContext)
        {
            _articlePriceService = articlePriceService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(ExportArticlePricesQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _articlePriceService.ExportArticlePricesAsync(_context, cancellationToken);
            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
