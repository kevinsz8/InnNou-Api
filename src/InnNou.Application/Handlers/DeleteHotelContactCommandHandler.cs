using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteHotelContactCommandHandler(IHotelContactService hotelContactService, IRequestContext context)
        : IRequestHandler<DeleteHotelContactCommandRequest, ApiResponse<DeleteHotelContactCommandResponse>>
    {
        public async Task<ApiResponse<DeleteHotelContactCommandResponse>> Handle(DeleteHotelContactCommandRequest request, CancellationToken cancellationToken)
        {
            var deleted = await hotelContactService.DeleteAsync(request.HotelContactToken, context, cancellationToken);
            if (!deleted)
                return ApiResponse<DeleteHotelContactCommandResponse>.FailureResponse("NOT_FOUND", "Hotel contact not found.", 404);

            return ApiResponse<DeleteHotelContactCommandResponse>.SuccessResponse(new DeleteHotelContactCommandResponse
            {
                HotelContactToken = request.HotelContactToken,
                Success = true
            });
        }
    }
}
