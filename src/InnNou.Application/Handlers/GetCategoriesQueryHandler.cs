using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetCategoriesQueryHandler(ICategoryService categoryService, IMapper mapper)
        : IRequestHandler<GetCategoriesQueryRequest, ApiResponse<GetCategoriesQueryResponse>>
    {
        public async Task<ApiResponse<GetCategoriesQueryResponse>> Handle(GetCategoriesQueryRequest request, CancellationToken cancellationToken)
        {
            var items = await categoryService.GetAllAsync(cancellationToken);
            var response = new GetCategoriesQueryResponse
            {
                Categories = mapper.MapList<Responses.Common.Category>(items)
            };
            return ApiResponse<GetCategoriesQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
