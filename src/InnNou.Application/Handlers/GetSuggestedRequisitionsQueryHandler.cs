using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSuggestedRequisitionsQueryHandler(IDepartmentParLevelService departmentParLevelService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetSuggestedRequisitionsQueryRequest, ApiResponse<GetSuggestedRequisitionsQueryResponse>>
    {
        public async Task<ApiResponse<GetSuggestedRequisitionsQueryResponse>> Handle(GetSuggestedRequisitionsQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await departmentParLevelService.GetSuggestedAsync(
                request.OrganizationToken, request.DepartmentToken, request.ArticleToken,
                request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetSuggestedRequisitionsQueryResponse
            {
                Items = mapper.MapList<Responses.Common.SuggestedRequisition>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetSuggestedRequisitionsQueryResponse>.SuccessResponse(response);
        }
    }
}
