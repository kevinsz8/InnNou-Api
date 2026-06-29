using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditSubFamilyCommandRequest : IRequest<ApiResponse<EditSubFamilyCommandResponse>>
    {
        public Guid SubFamilyToken { get; set; }
        public string Code { get; set; } = string.Empty;
    }
}
