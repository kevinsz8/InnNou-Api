using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetDepartmentParLevelByDepartmentAndArticleQueryHandler(IDepartmentParLevelService departmentParLevelService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetDepartmentParLevelByDepartmentAndArticleQueryRequest, ApiResponse<GetDepartmentParLevelByDepartmentAndArticleQueryResponse>>
    {
        public async Task<ApiResponse<GetDepartmentParLevelByDepartmentAndArticleQueryResponse>> Handle(GetDepartmentParLevelByDepartmentAndArticleQueryRequest request, CancellationToken cancellationToken)
        {
            if (request.DepartmentToken == Guid.Empty || request.ArticleToken == Guid.Empty)
                return ApiResponse<GetDepartmentParLevelByDepartmentAndArticleQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "DepartmentToken and ArticleToken are required.", 400);

            var result = await departmentParLevelService.GetByDepartmentAndArticleAsync(request.DepartmentToken, request.ArticleToken, context, cancellationToken);

            return ApiResponse<GetDepartmentParLevelByDepartmentAndArticleQueryResponse>.SuccessResponse(new GetDepartmentParLevelByDepartmentAndArticleQueryResponse
            {
                DepartmentParLevel = result is null ? null : mapper.Map<Responses.Common.DepartmentParLevel>(result)
            });
        }
    }
}
