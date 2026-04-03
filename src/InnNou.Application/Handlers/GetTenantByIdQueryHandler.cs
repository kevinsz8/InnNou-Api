using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Application.Responses.Common;
using InnNou.Domain.Persistence;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQueryRequest, ApiResponse<GetTenantByIdQueryResponse>>
    {
        private readonly ITenantService _tenantService;
        private readonly AutoMapper.IMapper _mapper;
        public GetTenantByIdQueryHandler(ITenantService tenantService, AutoMapper.IMapper mapper)
        {
            _tenantService = tenantService;
            _mapper = mapper;
        }
        public async Task<ApiResponse<GetTenantByIdQueryResponse>> Handle(GetTenantByIdQueryRequest request, CancellationToken cancellationToken)
        {
            var tenantDto = await _tenantService.GetTenantByIdAsync(request.TenantId, cancellationToken);
            if (tenantDto == null)
                return ApiResponse<GetTenantByIdQueryResponse>.FailureResponse("TENANT_NOT_FOUND", $"Tenant with id {request.TenantId} not found.");
            var tenant = _mapper.Map<Tenant>(tenantDto);
            var response = new InnNou.Application.Responses.GetTenantByIdQueryResponse { Tenant = tenant };
            return ApiResponse<InnNou.Application.Responses.GetTenantByIdQueryResponse>.SuccessResponse(response);
        }
    }
}
