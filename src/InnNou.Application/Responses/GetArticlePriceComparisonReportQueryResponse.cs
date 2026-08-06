using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetArticlePriceComparisonReportQueryResponse
    {
        public List<ArticlePriceComparison> Articles { get; set; } = [];
    }
}
