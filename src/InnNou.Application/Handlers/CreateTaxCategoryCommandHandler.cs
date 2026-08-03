using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateTaxCategoryCommandHandler(ITaxService taxService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateTaxCategoryCommandRequest, ApiResponse<CreateTaxCategoryCommandResponse>>
    {
        public async Task<ApiResponse<CreateTaxCategoryCommandResponse>> Handle(CreateTaxCategoryCommandRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                return ApiResponse<CreateTaxCategoryCommandResponse>.FailureResponse(ErrorCodes.TaxCategoryCodeRequired, "A category code is required.", 400);

            var result = await taxService.CreateTaxCategoryAsync(request.Code.Trim(), context, cancellationToken);

            var response = new CreateTaxCategoryCommandResponse { TaxCategory = mapper.Map<Responses.Common.TaxCategory>(result) };
            return ApiResponse<CreateTaxCategoryCommandResponse>.SuccessResponse(response, 201);
        }
    }
}
