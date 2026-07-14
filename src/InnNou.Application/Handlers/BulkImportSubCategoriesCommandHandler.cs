using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class BulkImportSubCategoriesCommandHandler : IRequestHandler<BulkImportSubCategoriesCommandRequest, ApiResponse<BulkImportSubCategoriesCommandResponse>>
    {
        private readonly ISubCategoryService _subCategoryService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;

        public BulkImportSubCategoriesCommandHandler(ISubCategoryService subCategoryService, IMapper mapper, IRequestContext requestContext)
        {
            _subCategoryService = subCategoryService;
            _mapper = mapper;
            _context = requestContext;
        }

        public async Task<ApiResponse<BulkImportSubCategoriesCommandResponse>> Handle(BulkImportSubCategoriesCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await _subCategoryService.BulkImportSubCategoriesAsync(request.FileBytes, _context, cancellationToken);
            var response = _mapper.Map<BulkImportSubCategoriesCommandResponse>(result);
            return ApiResponse<BulkImportSubCategoriesCommandResponse>.SuccessResponse(response);
        }
    }
}
