using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetFamilyDefaultTaxCategoryCommandHandler(IFamilyService familyService, IMapper mapper)
        : IRequestHandler<SetFamilyDefaultTaxCategoryCommandRequest, ApiResponse<SetFamilyDefaultTaxCategoryCommandResponse>>
    {
        public async Task<ApiResponse<SetFamilyDefaultTaxCategoryCommandResponse>> Handle(SetFamilyDefaultTaxCategoryCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await familyService.SetDefaultTaxCategoryAsync(request.FamilyToken, request.DefaultTaxCategoryToken, cancellationToken);
            if (result is null)
                return ApiResponse<SetFamilyDefaultTaxCategoryCommandResponse>.FailureResponse(ErrorCodes.FamilyNotFound, "Family not found.", 404);

            var response = new SetFamilyDefaultTaxCategoryCommandResponse { Family = mapper.Map<Responses.Common.Family>(result) };
            return ApiResponse<SetFamilyDefaultTaxCategoryCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
