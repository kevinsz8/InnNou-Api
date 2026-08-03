using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetGoodsReceiptTaxPreviewQueryResponse
    {
        public List<GoodsReceiptTaxPreviewLine> Lines { get; set; } = [];
    }
}
