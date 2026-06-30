using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetHotelContactsByHotelTokenQueryHandler(IHotelContactService hotelContactService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetHotelContactsByHotelTokenQueryRequest, ApiResponse<GetHotelContactsByHotelTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetHotelContactsByHotelTokenQueryResponse>> Handle(GetHotelContactsByHotelTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await hotelContactService.GetPagedByHotelTokenAsync(
                request.HotelToken, request.PageNumber, request.PageSize,
                request.SearchText, request.IncludeInactive, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetHotelContactsByHotelTokenQueryResponse
            {
                HotelContacts = mapper.MapList<Responses.Common.HotelContact>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetHotelContactsByHotelTokenQueryResponse>.SuccessResponse(response);
        }
    }
}
