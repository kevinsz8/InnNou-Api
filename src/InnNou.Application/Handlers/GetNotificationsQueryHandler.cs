using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetNotificationsQueryHandler(INotificationService notificationService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetNotificationsQueryRequest, ApiResponse<GetNotificationsQueryResponse>>
    {
        public async Task<ApiResponse<GetNotificationsQueryResponse>> Handle(GetNotificationsQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await notificationService.GetPagedAsync(request.UnreadOnly, request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetNotificationsQueryResponse
            {
                Notifications = mapper.MapList<Responses.Common.Notification>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetNotificationsQueryResponse>.SuccessResponse(response);
        }
    }
}
