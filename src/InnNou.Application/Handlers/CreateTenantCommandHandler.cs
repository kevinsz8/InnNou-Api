using AutoMapper;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Application.Responses.Common;
using InnNou.Domain.Persistence;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommandRequest, ApiResponse<CreateTenantCommandResponse>>
    {
        private readonly ITenantService _tenantService;
        private readonly IMapper _mapper;

        public CreateTenantCommandHandler(ITenantService tenantService, IMapper mapper)
        {
            _tenantService = tenantService;
            _mapper = mapper;
        }

        public async Task<ApiResponse<CreateTenantCommandResponse>> Handle(
            CreateTenantCommandRequest command,
            CancellationToken cancellationToken)
        {
            try
            {
                var tenantDto = await _tenantService.CreateTenantAsync(command.Name, command.Slug, cancellationToken);
                if (tenantDto == null)
                    return ApiResponse<CreateTenantCommandResponse>.FailureResponse("TENANT_CREATION_FAILED", "Tenant creation failed.");

                var response = new CreateTenantCommandResponse { Tenant = _mapper.Map<Tenant>(tenantDto) };
                return ApiResponse<CreateTenantCommandResponse>.SuccessResponse(response);
            }
            catch (System.Exception ex)
            {
                return ApiResponse<CreateTenantCommandResponse>.FailureResponse("EXCEPTION", ex.Message);
            }
        }
    }
}
