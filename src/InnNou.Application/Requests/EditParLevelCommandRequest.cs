using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditParLevelCommandRequest : IRequest<ApiResponse<EditParLevelCommandResponse>>
    {
        public Guid ParLevelToken { get; set; }
        public decimal MinimumQuantity { get; set; }
        public decimal ReorderQuantity { get; set; }
    }
}
