using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ExportCategoriesQueryHandler : IRequestHandler<ExportCategoriesQueryRequest, FileResult>
    {
        private readonly ICategoryService _categoryService;
        private readonly IRequestContext _context;

        public ExportCategoriesQueryHandler(ICategoryService categoryService, IRequestContext requestContext)
        {
            _categoryService = categoryService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(ExportCategoriesQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _categoryService.ExportCategoriesAsync(
                request.SearchText, request.IncludeInactive, _context, cancellationToken);

            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
