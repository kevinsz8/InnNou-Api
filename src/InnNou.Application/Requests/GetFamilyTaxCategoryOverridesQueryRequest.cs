using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetFamilyTaxCategoryOverridesQueryRequest : IRequest<ApiResponse<GetFamilyTaxCategoryOverridesQueryResponse>>
    {
        public Guid FamilyToken { get; set; }
    }
}
