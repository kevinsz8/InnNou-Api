using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ExportOrganizationsQueryHandler : IRequestHandler<ExportOrganizationsQueryRequest, FileResult>
    {
        private readonly IOrganizationService _organizationService;
        private readonly IRequestContext _context;

        public ExportOrganizationsQueryHandler(IOrganizationService organizationService, IRequestContext requestContext)
        {
            _organizationService = organizationService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(ExportOrganizationsQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _organizationService.ExportOrganizationsAsync(
                request.SearchField, request.SearchText, request.IncludeInactive, request.Language, _context, cancellationToken);

            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
