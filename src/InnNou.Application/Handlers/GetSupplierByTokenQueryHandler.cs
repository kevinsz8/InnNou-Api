using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Application.Responses.Common;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSupplierByTokenQueryHandler : IRequestHandler<GetSupplierByTokenQueryRequest, ApiResponse<GetSupplierByTokenQueryResponse>>
    {
        private readonly ISupplierService _supplierService;
        private readonly IRequestContext _context;
        private readonly IMapper _mapper;

        public GetSupplierByTokenQueryHandler(ISupplierService supplierService, IRequestContext context, IMapper mapper)
        {
            _supplierService = supplierService;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<GetSupplierByTokenQueryResponse>> Handle(GetSupplierByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var dto = await _supplierService.GetSupplierByTokenAsync(request.SupplierToken, _context, cancellationToken);

            if (dto is null)
                return ApiResponse<GetSupplierByTokenQueryResponse>.FailureResponse("SUPPLIER_NOT_FOUND", "Supplier not found or access denied.", 404);

            return ApiResponse<GetSupplierByTokenQueryResponse>.SuccessResponse(
                new GetSupplierByTokenQueryResponse { Supplier = _mapper.Map<Supplier>(dto) });
        }
    }
}
