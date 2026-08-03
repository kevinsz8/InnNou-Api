using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteFamilyTaxCategoryOverrideCommandHandler(ITaxService taxService, IRequestContext context)
        : IRequestHandler<DeleteFamilyTaxCategoryOverrideCommandRequest, ApiResponse<DeleteFamilyTaxCategoryOverrideCommandResponse>>
    {
        public async Task<ApiResponse<DeleteFamilyTaxCategoryOverrideCommandResponse>> Handle(DeleteFamilyTaxCategoryOverrideCommandRequest request, CancellationToken cancellationToken)
        {
            await taxService.DeleteFamilyTaxCategoryOverrideAsync(request.FamilyToken, request.TaxJurisdictionToken, context, cancellationToken);

            var response = new DeleteFamilyTaxCategoryOverrideCommandResponse { Deleted = true };
            return ApiResponse<DeleteFamilyTaxCategoryOverrideCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
