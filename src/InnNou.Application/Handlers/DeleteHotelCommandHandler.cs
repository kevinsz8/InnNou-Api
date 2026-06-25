using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteHotelCommandHandler : IRequestHandler<DeleteHotelCommandRequest, ApiResponse<DeleteHotelCommandResponse>>
    {
        private readonly IHotelService _hotelService;
        private readonly IRequestContext _context;

        public DeleteHotelCommandHandler(IHotelService hotelService, IRequestContext context)
        {
            _hotelService = hotelService;
            _context = context;
        }

        public async Task<ApiResponse<DeleteHotelCommandResponse>> Handle(DeleteHotelCommandRequest request, CancellationToken cancellationToken)
        {
            var success = await _hotelService.DeleteHotelAsync(request.HotelToken, _context, cancellationToken);

            if (!success)
                return ApiResponse<DeleteHotelCommandResponse>.FailureResponse("HOTEL_DELETE_FAILED", "Hotel could not be deleted.");

            return ApiResponse<DeleteHotelCommandResponse>.SuccessResponse(
                new DeleteHotelCommandResponse { HotelToken = request.HotelToken, Success = true });
        }
    }
}
