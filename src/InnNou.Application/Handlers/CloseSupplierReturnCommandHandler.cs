using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CloseSupplierReturnCommandHandler(ISupplierReturnService supplierReturnService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CloseSupplierReturnCommandRequest, ApiResponse<CloseSupplierReturnCommandResponse>>
    {
        public async Task<ApiResponse<CloseSupplierReturnCommandResponse>> Handle(CloseSupplierReturnCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.SupplierReturnToken == Guid.Empty)
                return ApiResponse<CloseSupplierReturnCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "SupplierReturnToken is required.", 400);

            if (string.IsNullOrWhiteSpace(request.ResolutionType))
                return ApiResponse<CloseSupplierReturnCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "ResolutionType is required.", 400);

            var result = await supplierReturnService.CloseAsync(request.SupplierReturnToken, request.ResolutionType, request.Notes, context, cancellationToken);
            if (result is null)
                return ApiResponse<CloseSupplierReturnCommandResponse>.FailureResponse(ErrorCodes.SupplierReturnNotFound, "Supplier return not found.", 404);

            return ApiResponse<CloseSupplierReturnCommandResponse>.SuccessResponse(new CloseSupplierReturnCommandResponse
            {
                SupplierReturn = mapper.Map<Responses.Common.SupplierReturn>(result)
            });
        }
    }
}
