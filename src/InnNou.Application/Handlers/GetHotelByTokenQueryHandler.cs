using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Application.Responses.Common;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetHotelByTokenQueryHandler : IRequestHandler<GetHotelByTokenQueryRequest, ApiResponse<GetHotelByTokenQueryResponse>>
    {
        private readonly IHotelService _hotelService;
        private readonly IRequestContext _context;
        private readonly IMapper _mapper;

        public GetHotelByTokenQueryHandler(IHotelService hotelService, IRequestContext context, IMapper mapper)
        {
            _hotelService = hotelService;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<GetHotelByTokenQueryResponse>> Handle(GetHotelByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var dto = await _hotelService.GetHotelByTokenAsync(request.HotelToken, _context, cancellationToken);

            if (dto is null)
                return ApiResponse<GetHotelByTokenQueryResponse>.FailureResponse("HOTEL_NOT_FOUND", "Hotel not found or access denied.", 404);

            return ApiResponse<GetHotelByTokenQueryResponse>.SuccessResponse(
                new GetHotelByTokenQueryResponse { Hotel = _mapper.Map<Hotel>(dto) });
        }
    }
}
