using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetCategoryImportTemplateQueryHandler : IRequestHandler<GetCategoryImportTemplateQueryRequest, FileResult>
    {
        private readonly ICategoryService _categoryService;
        private readonly IRequestContext _context;

        public GetCategoryImportTemplateQueryHandler(ICategoryService categoryService, IRequestContext requestContext)
        {
            _categoryService = categoryService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(GetCategoryImportTemplateQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _categoryService.GenerateCategoryImportTemplateAsync(request.Language, _context, cancellationToken);
            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
