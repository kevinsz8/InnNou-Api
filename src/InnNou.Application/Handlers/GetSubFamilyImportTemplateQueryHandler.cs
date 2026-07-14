using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSubFamilyImportTemplateQueryHandler : IRequestHandler<GetSubFamilyImportTemplateQueryRequest, FileResult>
    {
        private readonly ISubFamilyService _subFamilyService;
        private readonly IRequestContext _context;

        public GetSubFamilyImportTemplateQueryHandler(ISubFamilyService subFamilyService, IRequestContext requestContext)
        {
            _subFamilyService = subFamilyService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(GetSubFamilyImportTemplateQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _subFamilyService.GenerateSubFamilyImportTemplateAsync(_context, cancellationToken);
            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
