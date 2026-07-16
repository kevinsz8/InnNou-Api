using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetArticlesQueryRequest : IRequest<ApiResponse<GetArticlesQueryResponse>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? SupplierToken { get; set; }
        public Guid? FamilyToken { get; set; }
        public Guid? SubFamilyToken { get; set; }
        public string? SearchText { get; set; }
        public bool IncludeInactive { get; set; } = false;
        public bool FavoritesOnly { get; set; } = false;
    }
}
