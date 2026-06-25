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
            var exists = await _supplierService.SupplierExistsAsync(request.Name, cancellationToken);
            if (exists)
                return ApiResponse<CreateSupplierCommandResponse>.FailureResponse("SUPPLIER_ALREADY_EXISTS", "A supplier with this name already exists.");

            var dto = _mapper.Map<SupplierDto>(request);
            var created = await _supplierService.CreateSupplierAsync(dto, _context, cancellationToken);

            if (created is null)
                return ApiResponse<CreateSupplierCommandResponse>.FailureResponse("SUPPLIER_CREATION_FAILED", "Supplier could not be created.");

            return ApiResponse<CreateSupplierCommandResponse>.SuccessResponse(_mapper.Map<CreateSupplierCommandResponse>(created), 201);
        }
    }
}
