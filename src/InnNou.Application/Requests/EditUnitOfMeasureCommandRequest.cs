using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditUnitOfMeasureCommandRequest : IRequest<ApiResponse<EditUnitOfMeasureCommandResponse>>
    {
        public Guid UnitOfMeasureToken { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public int Decimals { get; set; }
    }
}
