using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetArticlePriceImportTemplateQueryHandler : IRequestHandler<GetArticlePriceImportTemplateQueryRequest, FileResult>
    {
        private readonly IArticlePriceService _articlePriceService;
        private readonly IRequestContext _context;

        public GetArticlePriceImportTemplateQueryHandler(IArticlePriceService articlePriceService, IRequestContext requestContext)
        {
            _articlePriceService = articlePriceService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(GetArticlePriceImportTemplateQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _articlePriceService.GenerateArticlePriceImportTemplateAsync(_context, cancellationToken);
            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
