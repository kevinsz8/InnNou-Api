using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetArticlePriceComparisonReportQueryRequest : IRequest<ApiResponse<GetArticlePriceComparisonReportQueryResponse>>
    {
        public Guid CategoryToken { get; set; }
        public Guid? SubCategoryToken { get; set; }
        public Guid? OrganizationToken { get; set; }
    }
}
