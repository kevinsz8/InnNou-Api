using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetFamilyImportTemplateQueryHandler : IRequestHandler<GetFamilyImportTemplateQueryRequest, FileResult>
    {
        private readonly IFamilyService _familyService;
        private readonly IRequestContext _context;

        public GetFamilyImportTemplateQueryHandler(IFamilyService familyService, IRequestContext requestContext)
        {
            _familyService = familyService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(GetFamilyImportTemplateQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _familyService.GenerateFamilyImportTemplateAsync(request.Language, _context, cancellationToken);
            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
