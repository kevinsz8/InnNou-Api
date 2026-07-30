using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetEligibleReturnLinesQueryResponse
    {
        public List<EligibleReturnLine> Lines { get; set; } = [];
    }
}
