using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditFamilyCommandRequest : IRequest<ApiResponse<EditFamilyCommandResponse>>
    {
        public Guid FamilyToken { get; set; }
        public string Code { get; set; } = string.Empty;
    }
}
