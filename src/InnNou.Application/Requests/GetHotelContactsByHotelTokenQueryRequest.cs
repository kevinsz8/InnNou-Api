using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetHotelContactsByHotelTokenQueryRequest : IRequest<ApiResponse<GetHotelContactsByHotelTokenQueryResponse>>
    {
        public Guid HotelToken { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchText { get; set; }
        public bool IncludeInactive { get; set; } = false;
    }
}
