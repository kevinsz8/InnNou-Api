using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetFamiliesQueryHandler(IFamilyService familyService, IMapper mapper)
        : IRequestHandler<GetFamiliesQueryRequest, ApiResponse<GetFamiliesQueryResponse>>
    {
        public async Task<ApiResponse<GetFamiliesQueryResponse>> Handle(GetFamiliesQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await familyService.GetPagedAsync(request.PageNumber, request.PageSize, request.SearchText, request.IncludeInactive, cancellationToken);
            var totalPages = result.TotalPages;
            var response = new GetFamiliesQueryResponse
            {
                Families = mapper.MapList<Responses.Common.Family>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetFamiliesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
