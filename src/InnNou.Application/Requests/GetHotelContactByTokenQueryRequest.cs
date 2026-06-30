using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetHotelContactByTokenQueryRequest : IRequest<ApiResponse<GetHotelContactByTokenQueryResponse>>
    {
        public Guid HotelContactToken { get; set; }
    }
}
