using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteSupplierCommandHandler : IRequestHandler<DeleteSupplierCommandRequest, ApiResponse<DeleteSupplierCommandResponse>>
    {
        private readonly ISupplierService _supplierService;
        private readonly IRequestContext _context;

        public DeleteSupplierCommandHandler(ISupplierService supplierService, IRequestContext context)
        {
            _supplierService = supplierService;
            _context = context;
        }

        public async Task<ApiResponse<DeleteSupplierCommandResponse>> Handle(DeleteSupplierCommandRequest request, CancellationToken cancellationToken)
        {
            var success = await _supplierService.DeleteSupplierAsync(request.SupplierToken, _context, cancellationToken);

            if (!success)
                return ApiResponse<DeleteSupplierCommandResponse>.FailureResponse(ErrorCodes.SupplierNotFound, "Supplier not found.", 404);

            return ApiResponse<DeleteSupplierCommandResponse>.SuccessResponse(
                new DeleteSupplierCommandResponse { SupplierToken = request.SupplierToken, Success = true });
        }
    }
}
