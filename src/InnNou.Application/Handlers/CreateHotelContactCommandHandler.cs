using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateHotelContactCommandHandler(IHotelContactService hotelContactService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateHotelContactCommandRequest, ApiResponse<CreateHotelContactCommandResponse>>
    {
        public async Task<ApiResponse<CreateHotelContactCommandResponse>> Handle(CreateHotelContactCommandRequest request, CancellationToken cancellationToken)
        {
            var dto = mapper.Map<HotelContactDto>(request);
            var result = await hotelContactService.CreateAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateHotelContactCommandResponse>.FailureResponse("INVALID_REQUEST", "Failed to create hotel contact.", 400);

            return ApiResponse<CreateHotelContactCommandResponse>.SuccessResponse(mapper.Map<CreateHotelContactCommandResponse>(result), 201);
        }
    }
}
