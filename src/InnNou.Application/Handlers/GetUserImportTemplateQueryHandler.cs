using InnNou.Application.Common;
using InnNou.Application.Persistence;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetUserImportTemplateQueryHandler : IRequestHandler<GetUserImportTemplateQueryRequest, FileResult>
    {
        private readonly IUserService _userService;
        private readonly IRequestContext _context;

        public GetUserImportTemplateQueryHandler(IUserService userService, IRequestContext requestContext)
        {
            _userService = userService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(GetUserImportTemplateQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _userService.GenerateUserImportTemplateAsync(_context, cancellationToken);
            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
