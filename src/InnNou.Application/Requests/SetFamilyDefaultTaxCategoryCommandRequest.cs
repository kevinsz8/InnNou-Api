using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetFamilyDefaultTaxCategoryCommandRequest : IRequest<ApiResponse<SetFamilyDefaultTaxCategoryCommandResponse>>
    {
        public Guid FamilyToken { get; set; }
        public Guid DefaultTaxCategoryToken { get; set; }
    }
}
