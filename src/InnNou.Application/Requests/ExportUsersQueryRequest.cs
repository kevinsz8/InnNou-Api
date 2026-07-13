using InnNou.Application.Common;
using MediatR;

namespace InnNou.Application.Requests
{
    public class ExportUsersQueryRequest : IRequest<FileResult>
    {
        public string? SearchField { get; set; }
        public string? SearchText { get; set; }
        public bool IncludeInactive { get; set; }
    }
}
