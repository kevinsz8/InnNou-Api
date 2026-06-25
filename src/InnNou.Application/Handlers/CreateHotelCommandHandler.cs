using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateHotelCommandHandler : IRequestHandler<CreateHotelCommandRequest, ApiResponse<CreateHotelCommandResponse>>
    {
        private readonly IHotelService _hotelService;
        private readonly AutoMapper.IMapper _mapper;
        private readonly IRequestContext _context;

        public CreateHotelCommandHandler(IHotelService hotelService, AutoMapper.IMapper mapper, IRequestContext context)
        {
            _hotelService = hotelService;
            _mapper = mapper;
            _context = context;
        }

        public async Task<ApiResponse<CreateHotelCommandResponse>> Handle(CreateHotelCommandRequest request, CancellationToken cancellationToken)
        {
            var exists = await _hotelService.HotelExistsByNameAsync(request.Name, cancellationToken);
            if (exists)
                return ApiResponse<CreateHotelCommandResponse>.FailureResponse("HOTEL_ALREADY_EXISTS", "A hotel with this name already exists.");

            var dto = _mapper.Map<HotelDto>(request);
            var created = await _hotelService.CreateHotelAsync(dto, _context, cancellationToken);

            if (created is null)
                return ApiResponse<CreateHotelCommandResponse>.FailureResponse("HOTEL_CREATION_FAILED", "Hotel could not be created.");

            return ApiResponse<CreateHotelCommandResponse>.SuccessResponse(_mapper.Map<CreateHotelCommandResponse>(created), 201);
        }
    }
}
