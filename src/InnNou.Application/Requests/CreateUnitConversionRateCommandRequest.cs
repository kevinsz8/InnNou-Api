using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateUnitConversionRateCommandRequest : IRequest<ApiResponse<CreateUnitConversionRateCommandResponse>>
    {
        public Guid FromUnitOfMeasureToken { get; set; }
        public Guid ToUnitOfMeasureToken { get; set; }
        public decimal Factor { get; set; }
    }
}
