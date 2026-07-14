using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class BulkImportArticlesCommandHandler : IRequestHandler<BulkImportArticlesCommandRequest, ApiResponse<BulkImportArticlesCommandResponse>>
    {
        private readonly IArticleService _articleService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;

        public BulkImportArticlesCommandHandler(IArticleService articleService, IMapper mapper, IRequestContext requestContext)
        {
            _articleService = articleService;
            _mapper = mapper;
            _context = requestContext;
        }

        public async Task<ApiResponse<BulkImportArticlesCommandResponse>> Handle(BulkImportArticlesCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await _articleService.BulkImportArticlesAsync(request.FileBytes, _context, cancellationToken);
            var response = _mapper.Map<BulkImportArticlesCommandResponse>(result);
            return ApiResponse<BulkImportArticlesCommandResponse>.SuccessResponse(response);
        }
    }
}
