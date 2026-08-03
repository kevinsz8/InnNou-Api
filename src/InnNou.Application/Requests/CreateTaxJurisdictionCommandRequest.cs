using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateTaxJurisdictionCommandRequest : IRequest<ApiResponse<CreateTaxJurisdictionCommandResponse>>
    {
        public string CountryCode { get; set; } = default!;
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
    }
}
