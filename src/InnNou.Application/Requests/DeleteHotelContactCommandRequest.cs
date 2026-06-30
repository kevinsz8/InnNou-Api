using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteHotelContactCommandRequest : IRequest<ApiResponse<DeleteHotelContactCommandResponse>>
    {
        public Guid HotelContactToken { get; set; }
    }
}
