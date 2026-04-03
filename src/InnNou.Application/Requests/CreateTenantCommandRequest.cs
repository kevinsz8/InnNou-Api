using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateTenantCommandRequest : IRequest<ApiResponse<CreateTenantCommandResponse>>
    {
        public string Name { get; set; } = default!;
        public string Slug { get; set; } = default!;
    }
}
