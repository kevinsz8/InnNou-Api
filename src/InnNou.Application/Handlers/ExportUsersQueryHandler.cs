using InnNou.Application.Common;
using InnNou.Application.Persistence;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ExportUsersQueryHandler : IRequestHandler<ExportUsersQueryRequest, FileResult>
    {
        private readonly IUserService _userService;
        private readonly IRequestContext _context;

        public ExportUsersQueryHandler(IUserService userService, IRequestContext requestContext)
        {
            _userService = userService;
            _context = requestContext;
        }

        public async Task<FileResult> Handle(ExportUsersQueryRequest request, CancellationToken cancellationToken)
        {
            var (fileBytes, fileName) = await _userService.ExportUsersAsync(
                request.SearchField, request.SearchText, request.IncludeInactive, request.Language, _context, cancellationToken);

            return new FileResult { FileBytes = fileBytes, FileName = fileName };
        }
    }
}
