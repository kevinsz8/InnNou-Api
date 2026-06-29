using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateFamilyCommandRequest : IRequest<ApiResponse<CreateFamilyCommandResponse>>
    {
        public string Code { get; set; } = string.Empty;
    }
}
