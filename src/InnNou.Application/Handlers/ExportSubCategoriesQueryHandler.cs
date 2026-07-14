using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ExportSubCategoriesQueryHandler : IRequestHandler<ExportSubCategoriesQueryRequest, FileResult>
    {
        private readonly ISubCategoryService _subCategoryService;
        private readonly IRequestContext _context;

        public ExportSubCategoriesQueryHandler(ISubCategoryService subCategoryService, IRequestContext requestContext)
        {
            _subCategoryService = subCategoryService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(ExportSubCategoriesQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _subCategoryService.ExportSubCategoriesAsync(
                request.SearchText, request.IncludeInactive, _context, cancellationToken);

            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
