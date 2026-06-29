using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditUnitTypeCommandRequest : IRequest<ApiResponse<EditUnitTypeCommandResponse>>
    {
        public Guid UnitTypeToken { get; set; }
        public string Code { get; set; } = string.Empty;
    }
}
