using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditUnitConversionRateCommandRequest : IRequest<ApiResponse<EditUnitConversionRateCommandResponse>>
    {
        public Guid UnitConversionRateToken { get; set; }
        public decimal Factor { get; set; }
    }
}
