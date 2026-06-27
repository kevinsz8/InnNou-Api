using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetActiveSubFamilyCommandHandler(ISubFamilyService subFamilyService, IMapper mapper)
        : IRequestHandler<SetActiveSubFamilyCommandRequest, ApiResponse<SetActiveSubFamilyCommandResponse>>
    {
        public async Task<ApiResponse<SetActiveSubFamilyCommandResponse>> Handle(SetActiveSubFamilyCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await subFamilyService.SetActiveAsync(request.SubFamilyToken, request.IsActive, cancellationToken);
            if (result is null)
                return ApiResponse<SetActiveSubFamilyCommandResponse>.FailureResponse("SUB_FAMILY_NOT_FOUND", "Sub-family not found.", 404);

            var response = new SetActiveSubFamilyCommandResponse { SubFamily = mapper.Map<Responses.Common.SubFamily>(result) };
            return ApiResponse<SetActiveSubFamilyCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
