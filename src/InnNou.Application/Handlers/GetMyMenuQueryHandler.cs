using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetMyMenuQueryHandler(IMenuService menuService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetMyMenuQueryRequest, ApiResponse<GetMyMenuQueryResponse>>
    {
        public async Task<ApiResponse<GetMyMenuQueryResponse>> Handle(GetMyMenuQueryRequest request, CancellationToken cancellationToken)
        {
            var items = await menuService.GetVisibleForContextAsync(context.RoleLevel, context.OrganizationId, context.SupplierId, cancellationToken);

            return ApiResponse<GetMyMenuQueryResponse>.SuccessResponse(
                new GetMyMenuQueryResponse { Items = mapper.MapList<Responses.Common.MenuItem>(items) }, 200);
        }
    }
}
