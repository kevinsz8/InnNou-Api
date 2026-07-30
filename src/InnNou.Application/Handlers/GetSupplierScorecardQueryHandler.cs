using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Application.Responses.Common;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSupplierScorecardQueryHandler : IRequestHandler<GetSupplierScorecardQueryRequest, ApiResponse<GetSupplierScorecardQueryResponse>>
    {
        private readonly ISupplierService _supplierService;
        private readonly IRequestContext _context;
        private readonly IMapper _mapper;

        public GetSupplierScorecardQueryHandler(ISupplierService supplierService, IRequestContext context, IMapper mapper)
        {
            _supplierService = supplierService;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<GetSupplierScorecardQueryResponse>> Handle(GetSupplierScorecardQueryRequest request, CancellationToken cancellationToken)
        {
            var dto = await _supplierService.GetScorecardAsync(request.SupplierToken, request.FromDate, request.ToDate, _context, cancellationToken);

            if (dto is null)
                return ApiResponse<GetSupplierScorecardQueryResponse>.FailureResponse(ErrorCodes.SupplierNotFound, "Supplier not found or access denied.", 404);

            return ApiResponse<GetSupplierScorecardQueryResponse>.SuccessResponse(
                new GetSupplierScorecardQueryResponse { Scorecard = _mapper.Map<SupplierScorecard>(dto) });
        }
    }
}
