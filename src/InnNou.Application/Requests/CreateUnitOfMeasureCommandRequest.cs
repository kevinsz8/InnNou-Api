using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateUnitOfMeasureCommandRequest : IRequest<ApiResponse<CreateUnitOfMeasureCommandResponse>>
    {
        public Guid UnitTypeToken { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public int Decimals { get; set; }
    }
}
