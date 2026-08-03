using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateTaxCategoryCommandRequest : IRequest<ApiResponse<CreateTaxCategoryCommandResponse>>
    {
        public string Code { get; set; } = default!;
    }
}
