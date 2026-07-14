using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetOrganizationImportTemplateQueryHandler : IRequestHandler<GetOrganizationImportTemplateQueryRequest, FileResult>
    {
        private readonly IOrganizationService _organizationService;
        private readonly IRequestContext _context;

        public GetOrganizationImportTemplateQueryHandler(IOrganizationService organizationService, IRequestContext requestContext)
        {
            _organizationService = organizationService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(GetOrganizationImportTemplateQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _organizationService.GenerateOrganizationImportTemplateAsync(_context, cancellationToken);
            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
