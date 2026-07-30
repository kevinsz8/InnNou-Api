using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CloseSupplierReturnCommandRequest : IRequest<ApiResponse<CloseSupplierReturnCommandResponse>>
    {
        public Guid SupplierReturnToken { get; set; }
        public string ResolutionType { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
