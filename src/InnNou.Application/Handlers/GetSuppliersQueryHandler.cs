using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Application.Responses.Common;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQueryRequest, ApiResponse<GetSuppliersQueryResponse>>
    {
        private readonly ISupplierService _supplierService;
        private readonly AutoMapper.IMapper _mapper;
        private readonly IRequestContext _context;

        public GetSuppliersQueryHandler(ISupplierService supplierService, AutoMapper.IMapper mapper, IRequestContext context)
        {
            _supplierService = supplierService;
            _mapper = mapper;
            _context = context;
        }

        public async Task<ApiResponse<GetSuppliersQueryResponse>> Handle(GetSuppliersQueryRequest request, CancellationToken cancellationToken)
        {
            var paged = await _supplierService.GetSuppliersAsync(
                request.PageNumber, request.PageSize,
                request.SearchField, request.SearchText,
                _context, cancellationToken);

            var totalPages = paged.TotalPages;
            var response = new GetSuppliersQueryResponse
            {
                Suppliers = _mapper.Map<List<Supplier>>(paged.Items),
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalPages = totalPages,
                HasNextPage = paged.PageNumber < totalPages,
                HasPreviousPage = paged.PageNumber > 1,
                NextPageNumber = paged.PageNumber < totalPages ? paged.PageNumber + 1 : (int?)null,
                PreviousPageNumber = paged.PageNumber > 1 ? paged.PageNumber - 1 : (int?)null
            };

            return ApiResponse<GetSuppliersQueryResponse>.SuccessResponse(response);
        }
    }
}
