using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetParLevelConfigurationQueryRequest : IRequest<ApiResponse<GetParLevelConfigurationQueryResponse>>
    {
        public Guid WarehouseToken { get; set; }
        public Guid ArticleToken { get; set; }
    }
}
