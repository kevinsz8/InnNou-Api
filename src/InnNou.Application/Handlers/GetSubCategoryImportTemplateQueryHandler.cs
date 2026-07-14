using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSubCategoryImportTemplateQueryHandler : IRequestHandler<GetSubCategoryImportTemplateQueryRequest, FileResult>
    {
        private readonly ISubCategoryService _subCategoryService;
        private readonly IRequestContext _context;

        public GetSubCategoryImportTemplateQueryHandler(ISubCategoryService subCategoryService, IRequestContext requestContext)
        {
            _subCategoryService = subCategoryService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(GetSubCategoryImportTemplateQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _subCategoryService.GenerateSubCategoryImportTemplateAsync(_context, cancellationToken);
            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
