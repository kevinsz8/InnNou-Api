using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateUnitTypeCommandRequest : IRequest<ApiResponse<CreateUnitTypeCommandResponse>>
    {
        public string Code { get; set; } = string.Empty;
    }
}
