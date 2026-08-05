using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetDepartmentsQueryHandler(IDepartmentService departmentService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetDepartmentsQueryRequest, ApiResponse<GetDepartmentsQueryResponse>>
    {
        public async Task<ApiResponse<GetDepartmentsQueryResponse>> Handle(GetDepartmentsQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await departmentService.GetPagedByOrganizationTokenAsync(
                request.OrganizationToken, request.PageNumber, request.PageSize,
                request.SearchText, request.IncludeInactive, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetDepartmentsQueryResponse
            {
                Departments = mapper.MapList<Responses.Common.Department>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetDepartmentsQueryResponse>.SuccessResponse(response);
        }
    }
}
