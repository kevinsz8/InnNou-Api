using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class BulkImportCategoriesCommandHandler : IRequestHandler<BulkImportCategoriesCommandRequest, ApiResponse<BulkImportCategoriesCommandResponse>>
    {
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;

        public BulkImportCategoriesCommandHandler(ICategoryService categoryService, IMapper mapper, IRequestContext requestContext)
        {
            _categoryService = categoryService;
            _mapper = mapper;
            _context = requestContext;
        }

        public async Task<ApiResponse<BulkImportCategoriesCommandResponse>> Handle(BulkImportCategoriesCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await _categoryService.BulkImportCategoriesAsync(request.FileBytes, _context, cancellationToken);
            var response = _mapper.Map<BulkImportCategoriesCommandResponse>(result);
            return ApiResponse<BulkImportCategoriesCommandResponse>.SuccessResponse(response);
        }
    }
}
