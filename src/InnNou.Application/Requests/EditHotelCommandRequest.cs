using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditHotelCommandRequest : IRequest<ApiResponse<EditHotelCommandResponse>>
    {
        public Guid HotelToken { get; set; }
        public string? Name { get; set; }
        public string? LegalName { get; set; }
        public string? Code { get; set; }
        public int? ParentHotelId { get; set; }
        public string? TimeZone { get; set; }
        public string? CurrencyCode { get; set; }
        public string? LanguageCode { get; set; }
    }
}
