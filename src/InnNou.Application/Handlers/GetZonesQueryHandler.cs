using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetZonesQueryHandler(IZoneService zoneService, IMapper mapper)
        : IRequestHandler<GetZonesQueryRequest, ApiResponse<GetZonesQueryResponse>>
    {
        public async Task<ApiResponse<GetZonesQueryResponse>> Handle(GetZonesQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await zoneService.GetPagedAsync(request.PageNumber, request.PageSize, request.CountryCode, request.SearchText, request.IncludeInactive, cancellationToken);
            var totalPages = result.TotalPages;
            var response = new GetZonesQueryResponse
            {
                Zones = mapper.MapList<Responses.Common.Zone>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.PageNumber < totalPages,
                HasPreviousPage = request.PageNumber > 1,
                NextPageNumber = request.PageNumber < totalPages ? request.PageNumber + 1 : (int?)null,
                PreviousPageNumber = request.PageNumber > 1 ? request.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetZonesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
