using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteSupplierLogoCommandHandler(ISupplierService supplierService, IRequestContext context)
        : IRequestHandler<DeleteSupplierLogoCommandRequest, ApiResponse<DeleteSupplierLogoCommandResponse>>
    {
        public async Task<ApiResponse<DeleteSupplierLogoCommandResponse>> Handle(DeleteSupplierLogoCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.SupplierToken == Guid.Empty)
                return ApiResponse<DeleteSupplierLogoCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "SupplierToken is required.", 400);

            var updated = await supplierService.DeleteLogoAsync(request.SupplierToken, context, cancellationToken);

            if (updated is null)
                return ApiResponse<DeleteSupplierLogoCommandResponse>.FailureResponse(ErrorCodes.SupplierNotFound, "Supplier not found.", 404);

            return ApiResponse<DeleteSupplierLogoCommandResponse>.SuccessResponse(new DeleteSupplierLogoCommandResponse
            {
                SupplierToken = updated.SupplierToken
            });
        }
    }
}
