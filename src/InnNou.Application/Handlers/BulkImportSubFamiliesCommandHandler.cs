using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class BulkImportSubFamiliesCommandHandler : IRequestHandler<BulkImportSubFamiliesCommandRequest, ApiResponse<BulkImportSubFamiliesCommandResponse>>
    {
        private readonly ISubFamilyService _subFamilyService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;

        public BulkImportSubFamiliesCommandHandler(ISubFamilyService subFamilyService, IMapper mapper, IRequestContext requestContext)
        {
            _subFamilyService = subFamilyService;
            _mapper = mapper;
            _context = requestContext;
        }

        public async Task<ApiResponse<BulkImportSubFamiliesCommandResponse>> Handle(BulkImportSubFamiliesCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await _subFamilyService.BulkImportSubFamiliesAsync(request.FileBytes, _context, cancellationToken);
            var response = _mapper.Map<BulkImportSubFamiliesCommandResponse>(result);
            return ApiResponse<BulkImportSubFamiliesCommandResponse>.SuccessResponse(response);
        }
    }
}
