using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ExportSubFamiliesQueryHandler : IRequestHandler<ExportSubFamiliesQueryRequest, FileResult>
    {
        private readonly ISubFamilyService _subFamilyService;
        private readonly IRequestContext _context;

        public ExportSubFamiliesQueryHandler(ISubFamilyService subFamilyService, IRequestContext requestContext)
        {
            _subFamilyService = subFamilyService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(ExportSubFamiliesQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _subFamilyService.ExportSubFamiliesAsync(
                request.SearchText, request.IncludeInactive, request.Language, _context, cancellationToken);

            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
