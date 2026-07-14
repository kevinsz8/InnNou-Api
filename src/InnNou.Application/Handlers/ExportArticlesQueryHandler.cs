using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ExportArticlesQueryHandler : IRequestHandler<ExportArticlesQueryRequest, FileResult>
    {
        private readonly IArticleService _articleService;
        private readonly IRequestContext _context;

        public ExportArticlesQueryHandler(IArticleService articleService, IRequestContext requestContext)
        {
            _articleService = articleService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(ExportArticlesQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _articleService.ExportArticlesAsync(
                request.SearchText, request.IncludeInactive, request.Language, _context, cancellationToken);

            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
