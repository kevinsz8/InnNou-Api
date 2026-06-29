using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditSubCategoryCommandRequest : IRequest<ApiResponse<EditSubCategoryCommandResponse>>
    {
        public Guid SubCategoryToken { get; set; }
        public string Code { get; set; } = string.Empty;
    }
}
