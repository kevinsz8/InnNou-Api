using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ExportFamiliesQueryHandler : IRequestHandler<ExportFamiliesQueryRequest, FileResult>
    {
        private readonly IFamilyService _familyService;
        private readonly IRequestContext _context;

        public ExportFamiliesQueryHandler(IFamilyService familyService, IRequestContext requestContext)
        {
            _familyService = familyService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(ExportFamiliesQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _familyService.ExportFamiliesAsync(
                request.SearchText, request.IncludeInactive, _context, cancellationToken);

            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
