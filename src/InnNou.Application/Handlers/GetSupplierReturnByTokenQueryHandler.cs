using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSupplierReturnByTokenQueryHandler(ISupplierReturnService supplierReturnService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetSupplierReturnByTokenQueryRequest, ApiResponse<GetSupplierReturnByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetSupplierReturnByTokenQueryResponse>> Handle(GetSupplierReturnByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await supplierReturnService.GetByTokenAsync(request.SupplierReturnToken, context, cancellationToken);

            if (result is null)
                return ApiResponse<GetSupplierReturnByTokenQueryResponse>.FailureResponse(ErrorCodes.SupplierReturnNotFound, "Supplier return not found or access denied.", 404);

            return ApiResponse<GetSupplierReturnByTokenQueryResponse>.SuccessResponse(new GetSupplierReturnByTokenQueryResponse
            {
                SupplierReturn = mapper.Map<Responses.Common.SupplierReturn>(result)
            });
        }
    }
}
