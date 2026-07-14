using InnNou.Application.Common;
using MediatR;

namespace InnNou.Application.Requests
{
    public class ExportOrganizationsQueryRequest : IRequest<FileResult>
    {
        public string? SearchField { get; set; }
        public string? SearchText { get; set; }
        public bool IncludeInactive { get; set; }
    }
}
