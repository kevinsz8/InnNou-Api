using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateSubFamilyCommandRequest : IRequest<ApiResponse<CreateSubFamilyCommandResponse>>
    {
        public Guid FamilyToken { get; set; }
        public string Code { get; set; } = string.Empty;
    }
}
