using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetArticleImportTemplateQueryHandler : IRequestHandler<GetArticleImportTemplateQueryRequest, FileResult>
    {
        private readonly IArticleService _articleService;
        private readonly IRequestContext _context;

        public GetArticleImportTemplateQueryHandler(IArticleService articleService, IRequestContext requestContext)
        {
            _articleService = articleService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(GetArticleImportTemplateQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _articleService.GenerateArticleImportTemplateAsync(request.Language, _context, cancellationToken);
            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
