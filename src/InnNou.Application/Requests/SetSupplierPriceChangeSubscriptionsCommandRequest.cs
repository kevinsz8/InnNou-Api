using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetSupplierPriceChangeSubscriptionsCommandRequest : IRequest<ApiResponse<SetSupplierPriceChangeSubscriptionsCommandResponse>>
    {
        public List<Guid> SupplierTokens { get; set; } = [];
    }
}
