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
            var result = await familyService.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
            var totalPages = result.TotalPages;
            var response = new GetFamiliesQueryResponse
            {
                Families = mapper.MapList<Responses.Common.Family>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.PageNumber < totalPages,
                HasPreviousPage = request.PageNumber > 1,
                NextPageNumber = request.PageNumber < totalPages ? request.PageNumber + 1 : (int?)null,
                PreviousPageNumber = request.PageNumber > 1 ? request.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetFamiliesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
