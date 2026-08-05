using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetRequisitionsQueryHandler(IRequisitionService requisitionService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetRequisitionsQueryRequest, ApiResponse<GetRequisitionsQueryResponse>>
    {
        public async Task<ApiResponse<GetRequisitionsQueryResponse>> Handle(GetRequisitionsQueryRequest request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.Status) && !RequisitionStatusCodes.TryFromCode(request.Status, out _))
                return ApiResponse<GetRequisitionsQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Invalid Status filter.", 400);

            var result = await requisitionService.GetPagedAsync(
                request.OrganizationToken, request.WarehouseToken, request.DepartmentToken, request.Status,
                request.FromDate, request.ToDate, request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetRequisitionsQueryResponse
            {
                Requisitions = mapper.MapList<Responses.Common.Requisition>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetRequisitionsQueryResponse>.SuccessResponse(response);
        }
    }
}
