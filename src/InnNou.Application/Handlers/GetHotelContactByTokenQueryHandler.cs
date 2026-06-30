using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetHotelContactByTokenQueryHandler(IHotelContactService hotelContactService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetHotelContactByTokenQueryRequest, ApiResponse<GetHotelContactByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetHotelContactByTokenQueryResponse>> Handle(GetHotelContactByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var contact = await hotelContactService.GetByTokenAsync(request.HotelContactToken, context, cancellationToken);
            if (contact is null)
                return ApiResponse<GetHotelContactByTokenQueryResponse>.FailureResponse("NOT_FOUND", "Hotel contact not found.", 404);

            return ApiResponse<GetHotelContactByTokenQueryResponse>.SuccessResponse(new GetHotelContactByTokenQueryResponse
            {
                HotelContact = mapper.Map<Responses.Common.HotelContact>(contact)
            });
        }
    }
}
