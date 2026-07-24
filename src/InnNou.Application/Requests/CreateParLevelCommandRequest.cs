using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateParLevelCommandRequest : IRequest<ApiResponse<CreateParLevelCommandResponse>>
    {
        public Guid WarehouseToken { get; set; }
        public Guid ArticleToken { get; set; }
        public decimal MinimumQuantity { get; set; }
        public decimal ReorderQuantity { get; set; }
    }
}
