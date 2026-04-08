using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Application.Responses.Common;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetHotelsHandler : IRequestHandler<GetHotelsQueryRequest, ApiResponse<GetHotelsQueryResponse>>
    {
        private readonly IHotelService _hotelService;
        private readonly IRequestContext _context;
        private readonly AutoMapper.IMapper _mapper;

        public GetHotelsHandler(IHotelService hotelService, IRequestContext context, AutoMapper.IMapper mapper)
        {
            _hotelService = hotelService;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<GetHotelsQueryResponse>> Handle(GetHotelsQueryRequest request, CancellationToken cancellationToken)
        {
            var resultHotels = await _hotelService.GetHotelsAsync(request.PageNumber, request.PageSize, request.SearchField, request.SearchText, _context, cancellationToken);
            var hotels = _mapper.Map<List<Hotel>>(resultHotels.Items);
            var totalPages = resultHotels.TotalPages;
            var response = new GetHotelsQueryResponse
            {
                Hotels = hotels,
                TotalCount = resultHotels.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.PageNumber < totalPages,
                HasPreviousPage = request.PageNumber > 1,
                NextPageNumber = request.PageNumber < totalPages ? request.PageNumber + 1 : (int?)null,
                PreviousPageNumber = request.PageNumber > 1 ? request.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetHotelsQueryResponse>.SuccessResponse(new GetHotelsQueryResponse
            {
                Hotels = hotels
            });
        }
    }
}
