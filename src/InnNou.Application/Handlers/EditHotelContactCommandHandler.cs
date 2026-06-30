using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditHotelContactCommandHandler(IHotelContactService hotelContactService, IMapper mapper, IRequestContext context)
        : IRequestHandler<EditHotelContactCommandRequest, ApiResponse<EditHotelContactCommandResponse>>
    {
        public async Task<ApiResponse<EditHotelContactCommandResponse>> Handle(EditHotelContactCommandRequest request, CancellationToken cancellationToken)
        {
            var dto = mapper.Map<HotelContactDto>(request);
            var result = await hotelContactService.EditAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<EditHotelContactCommandResponse>.FailureResponse("NOT_FOUND", "Hotel contact not found.", 404);

            return ApiResponse<EditHotelContactCommandResponse>.SuccessResponse(mapper.Map<EditHotelContactCommandResponse>(result));
        }
    }
}
