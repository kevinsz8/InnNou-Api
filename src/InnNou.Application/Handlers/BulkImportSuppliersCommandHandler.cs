using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class BulkImportSuppliersCommandHandler : IRequestHandler<BulkImportSuppliersCommandRequest, ApiResponse<BulkImportSuppliersCommandResponse>>
    {
        private readonly ISupplierService _supplierService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;

        public BulkImportSuppliersCommandHandler(ISupplierService supplierService, IMapper mapper, IRequestContext requestContext)
        {
            _supplierService = supplierService;
            _mapper = mapper;
            _context = requestContext;
        }

        public async Task<ApiResponse<BulkImportSuppliersCommandResponse>> Handle(BulkImportSuppliersCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await _supplierService.BulkImportSuppliersAsync(request.FileBytes, _context, cancellationToken);
            var response = _mapper.Map<BulkImportSuppliersCommandResponse>(result);
            return ApiResponse<BulkImportSuppliersCommandResponse>.SuccessResponse(response);
        }
    }
}
