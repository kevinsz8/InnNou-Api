using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommandRequest, ApiResponse<CreateSupplierCommandResponse>>
    {
        private readonly ISupplierService _supplierService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;

        public CreateSupplierCommandHandler(ISupplierService supplierService, IMapper mapper, IRequestContext context)
        {
            _supplierService = supplierService;
            _mapper = mapper;
            _context = context;
        }

        public async Task<ApiResponse<CreateSupplierCommandResponse>> Handle(CreateSupplierCommandRequest request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.SupplierType) && !SupplierTypeCodes.IsValid(request.SupplierType))
                return ApiResponse<CreateSupplierCommandResponse>.FailureResponse(ErrorCodes.SupplierInvalidType, "SupplierType must be Product, Service, or Mixed.", 400);

            // Name-uniqueness is scope-aware (global vs. a specific owning organization) and can
            // only be resolved after authorization runs, so the check now lives inside
            // SupplierService.CreateSupplierAsync itself rather than here.
            var dto = _mapper.Map<SupplierDto>(request);
            if (!string.IsNullOrWhiteSpace(dto.SupplierType))
                dto.SupplierType = dto.SupplierType.Trim().ToUpperInvariant();

            var created = await _supplierService.CreateSupplierAsync(dto, _context, cancellationToken);

            if (created is null)
                return ApiResponse<CreateSupplierCommandResponse>.FailureResponse(ErrorCodes.SupplierCreationFailed, "Supplier could not be created.");

            return ApiResponse<CreateSupplierCommandResponse>.SuccessResponse(_mapper.Map<CreateSupplierCommandResponse>(created), 201);
        }
    }
}
