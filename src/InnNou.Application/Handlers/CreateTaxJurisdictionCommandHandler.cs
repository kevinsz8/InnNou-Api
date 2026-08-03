using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateTaxJurisdictionCommandHandler(ITaxService taxService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateTaxJurisdictionCommandRequest, ApiResponse<CreateTaxJurisdictionCommandResponse>>
    {
        public async Task<ApiResponse<CreateTaxJurisdictionCommandResponse>> Handle(CreateTaxJurisdictionCommandRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.CountryCode) || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
                return ApiResponse<CreateTaxJurisdictionCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "CountryCode, Code and Name are required.", 400);

            var result = await taxService.CreateTaxJurisdictionAsync(request.CountryCode.Trim(), request.Code.Trim(), request.Name.Trim(), context, cancellationToken);

            var response = new CreateTaxJurisdictionCommandResponse { TaxJurisdiction = mapper.Map<Responses.Common.TaxJurisdiction>(result) };
            return ApiResponse<CreateTaxJurisdictionCommandResponse>.SuccessResponse(response, 201);
        }
    }
}
