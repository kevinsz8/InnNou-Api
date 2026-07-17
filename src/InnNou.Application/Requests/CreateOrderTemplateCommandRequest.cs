using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateOrderTemplateCommandRequest : IRequest<ApiResponse<CreateOrderTemplateCommandResponse>>
    {
        public Guid WarehouseToken { get; set; }
        public string Name { get; set; } = default!;
    }
}
