using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class BulkImportFamiliesCommandHandler : IRequestHandler<BulkImportFamiliesCommandRequest, ApiResponse<BulkImportFamiliesCommandResponse>>
    {
        private readonly IFamilyService _familyService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;

        public BulkImportFamiliesCommandHandler(IFamilyService familyService, IMapper mapper, IRequestContext requestContext)
        {
            _familyService = familyService;
            _mapper = mapper;
            _context = requestContext;
        }

        public async Task<ApiResponse<BulkImportFamiliesCommandResponse>> Handle(BulkImportFamiliesCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await _familyService.BulkImportFamiliesAsync(request.FileBytes, _context, cancellationToken);
            var response = _mapper.Map<BulkImportFamiliesCommandResponse>(result);
            return ApiResponse<BulkImportFamiliesCommandResponse>.SuccessResponse(response);
        }
    }
}
