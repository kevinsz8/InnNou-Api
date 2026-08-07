using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetUnitTypesQueryHandler(IUnitTypeService unitTypeService, IMapper mapper)
        : IRequestHandler<GetUnitTypesQueryRequest, ApiResponse<GetUnitTypesQueryResponse>>
    {
        public async Task<ApiResponse<GetUnitTypesQueryResponse>> Handle(GetUnitTypesQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await unitTypeService.GetPagedAsync(request.PageNumber, request.PageSize, request.IncludeInactive, cancellationToken);
            var totalPages = result.TotalPages;
            var response = new GetUnitTypesQueryResponse
            {
                UnitTypes = mapper.MapList<Responses.Common.UnitType>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetUnitTypesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
