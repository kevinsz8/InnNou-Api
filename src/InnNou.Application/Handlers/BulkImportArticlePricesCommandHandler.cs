using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class BulkImportArticlePricesCommandHandler : IRequestHandler<BulkImportArticlePricesCommandRequest, ApiResponse<BulkImportArticlePricesCommandResponse>>
    {
        private readonly IArticlePriceService _articlePriceService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;

        public BulkImportArticlePricesCommandHandler(IArticlePriceService articlePriceService, IMapper mapper, IRequestContext requestContext)
        {
            _articlePriceService = articlePriceService;
            _mapper = mapper;
            _context = requestContext;
        }

        public async Task<ApiResponse<BulkImportArticlePricesCommandResponse>> Handle(BulkImportArticlePricesCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await _articlePriceService.BulkImportArticlePricesAsync(request.FileBytes, _context, cancellationToken);
            var response = _mapper.Map<BulkImportArticlePricesCommandResponse>(result);
            return ApiResponse<BulkImportArticlePricesCommandResponse>.SuccessResponse(response);
        }
    }
}
