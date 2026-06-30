using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSubFamiliesQueryHandler(ISubFamilyService subFamilyService, IFamilyService familyService, IMapper mapper)
        : IRequestHandler<GetSubFamiliesQueryRequest, ApiResponse<GetSubFamiliesQueryResponse>>
    {
        public async Task<ApiResponse<GetSubFamiliesQueryResponse>> Handle(GetSubFamiliesQueryRequest request, CancellationToken cancellationToken)
        {
            int? familyId = null;
            if (request.FamilyToken.HasValue)
            {
                var family = await familyService.GetByTokenAsync(request.FamilyToken.Value, cancellationToken);
                if (family is null)
                    return ApiResponse<GetSubFamiliesQueryResponse>.FailureResponse("FAMILY_NOT_FOUND", "Family not found.", 404);
                familyId = family.FamilyId;
            }

            var result = await subFamilyService.GetPagedAsync(request.PageNumber, request.PageSize, familyId, request.SearchText, request.IncludeInactive, cancellationToken);
            var totalPages = result.TotalPages;
            var response = new GetSubFamiliesQueryResponse
            {
                SubFamilies = mapper.MapList<Responses.Common.SubFamily>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.PageNumber < totalPages,
                HasPreviousPage = request.PageNumber > 1,
                NextPageNumber = request.PageNumber < totalPages ? request.PageNumber + 1 : (int?)null,
                PreviousPageNumber = request.PageNumber > 1 ? request.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetSubFamiliesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
