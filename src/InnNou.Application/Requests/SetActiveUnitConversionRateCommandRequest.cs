using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetActiveUnitConversionRateCommandRequest : IRequest<ApiResponse<SetActiveUnitConversionRateCommandResponse>>
    {
        public Guid UnitConversionRateToken { get; set; }
        public bool IsActive { get; set; }
    }
}
