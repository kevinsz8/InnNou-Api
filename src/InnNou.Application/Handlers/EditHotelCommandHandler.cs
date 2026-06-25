using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditHotelCommandHandler : IRequestHandler<EditHotelCommandRequest, ApiResponse<EditHotelCommandResponse>>
    {
        private readonly IHotelService _hotelService;
        private readonly AutoMapper.IMapper _mapper;
        private readonly IRequestContext _context;

        public EditHotelCommandHandler(IHotelService hotelService, AutoMapper.IMapper mapper, IRequestContext context)
        {
            _hotelService = hotelService;
            _mapper = mapper;
            _context = context;
        }

        public async Task<ApiResponse<EditHotelCommandResponse>> Handle(EditHotelCommandRequest request, CancellationToken cancellationToken)
        {
            var dto = _mapper.Map<HotelDto>(request);
            var updated = await _hotelService.EditHotelAsync(dto, _context, cancellationToken);

            if (updated is null)
                return ApiResponse<EditHotelCommandResponse>.FailureResponse("HOTEL_EDIT_FAILED", "Hotel could not be updated.");

            return ApiResponse<EditHotelCommandResponse>.SuccessResponse(_mapper.Map<EditHotelCommandResponse>(updated));
        }
    }
}
