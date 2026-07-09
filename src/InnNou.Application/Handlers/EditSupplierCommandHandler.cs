using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditSupplierCommandHandler : IRequestHandler<EditSupplierCommandRequest, ApiResponse<EditSupplierCommandResponse>>
    {
        private readonly ISupplierService _supplierService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _context;

        public EditSupplierCommandHandler(ISupplierService supplierService, IMapper mapper, IRequestContext context)
        {
            _supplierService = supplierService;
            _mapper = mapper;
            _context = context;
        }

        public async Task<ApiResponse<EditSupplierCommandResponse>> Handle(EditSupplierCommandRequest request, CancellationToken cancellationToken)
        {
            var dto = _mapper.Map<SupplierDto>(request);
            var updated = await _supplierService.EditSupplierAsync(dto, _context, cancellationToken);

            if (updated is null)
                return ApiResponse<EditSupplierCommandResponse>.FailureResponse(ErrorCodes.SupplierNotFound, "Supplier not found.", 404);

            return ApiResponse<EditSupplierCommandResponse>.SuccessResponse(_mapper.Map<EditSupplierCommandResponse>(updated));
        }
    }
}
