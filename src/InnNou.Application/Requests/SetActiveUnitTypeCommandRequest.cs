using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetActiveUnitTypeCommandRequest : IRequest<ApiResponse<SetActiveUnitTypeCommandResponse>>
    {
        public Guid UnitTypeToken { get; set; }
        public bool IsActive { get; set; }
    }
}
