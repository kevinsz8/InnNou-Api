using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetBelowParQueryRequest : IRequest<ApiResponse<GetBelowParQueryResponse>>
    {
        public Guid? WarehouseToken { get; set; }
        public string? SearchText { get; set; }
        public Guid? FamilyToken { get; set; }
        public Guid? SubFamilyToken { get; set; }
        public Guid? CategoryToken { get; set; }
        public Guid? SubCategoryToken { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
