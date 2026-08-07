using InnNou.Application.Common;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using MediatR;

namespace InnNou.Application.Requests
{
    public class AddOrderLinesCommandRequest : IRequest<ApiResponse<AddOrderLinesCommandResponse>>
    {
        public Guid OrderToken { get; set; }
        public List<AddOrderLineInputDto> Lines { get; set; } = new();
    }
}
