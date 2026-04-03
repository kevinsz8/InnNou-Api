using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Application.Responses.Common;
using InnNou.Domain.Persistence;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQueryRequest, ApiResponse<GetTenantsQueryResponse>>
    {
        private readonly ITenantService _tenantService;
        private readonly AutoMapper.IMapper _mapper;
        public GetTenantsQueryHandler(ITenantService tenantService, AutoMapper.IMapper mapper)
        {
            _tenantService = tenantService;
            _mapper = mapper;
        }
        public async Task<ApiResponse<GetTenantsQueryResponse>> Handle(GetTenantsQueryRequest request, CancellationToken cancellationToken)
        {
            var tenantDtos = await _tenantService.GetTenantsAsync(cancellationToken);
            var tenants = _mapper.Map<List<Tenant>>(tenantDtos);
            var response = new InnNou.Application.Responses.GetTenantsQueryResponse { Tenants = tenants };
            return ApiResponse<InnNou.Application.Responses.GetTenantsQueryResponse>.SuccessResponse(response);
        }
    }
}
