using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class UploadSupplierLogoCommandRequest : IRequest<ApiResponse<UploadSupplierLogoCommandResponse>>
    {
        public Guid SupplierToken { get; set; }
        public required byte[] FileBytes { get; set; }
        public required string FileName { get; set; }
    }
}
