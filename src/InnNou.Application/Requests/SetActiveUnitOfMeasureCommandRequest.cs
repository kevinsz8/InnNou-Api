using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetActiveUnitOfMeasureCommandRequest : IRequest<ApiResponse<SetActiveUnitOfMeasureCommandResponse>>
    {
        public Guid UnitOfMeasureToken { get; set; }
        public bool IsActive { get; set; }
    }
}
