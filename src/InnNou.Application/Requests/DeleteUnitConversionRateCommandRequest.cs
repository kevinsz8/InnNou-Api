using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteUnitConversionRateCommandRequest : IRequest<ApiResponse<DeleteUnitConversionRateCommandResponse>>
    {
        public Guid UnitConversionRateToken { get; set; }
    }
}
