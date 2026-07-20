using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditArticleCommandRequest : IRequest<ApiResponse<EditArticleCommandResponse>>
    {
        public Guid ArticleToken { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SupplierSku { get; set; }
        public string? Barcode { get; set; }
        public string? Brand { get; set; }
        public Guid? FamilyToken { get; set; }
        public Guid? SubFamilyToken { get; set; }
        public Guid PurchaseUnitToken { get; set; }
        public List<ArticlePackagingLevelRequest> PackagingLevels { get; set; } = [];
        public decimal? MinimumOrderQty { get; set; }
        public int? LeadTimeDays { get; set; }
    }
}
