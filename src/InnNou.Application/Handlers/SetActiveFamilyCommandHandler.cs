using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetActiveFamilyCommandHandler(IFamilyService familyService, IMapper mapper)
        : IRequestHandler<SetActiveFamilyCommandRequest, ApiResponse<SetActiveFamilyCommandResponse>>
    {
        public async Task<ApiResponse<SetActiveFamilyCommandResponse>> Handle(SetActiveFamilyCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await familyService.SetActiveAsync(request.FamilyToken, request.IsActive, cancellationToken);
            if (result is null)
                return ApiResponse<SetActiveFamilyCommandResponse>.FailureResponse("FAMILY_NOT_FOUND", "Family not found.", 404);

            var response = new SetActiveFamilyCommandResponse { Family = mapper.Map<Responses.Common.Family>(result) };
            return ApiResponse<SetActiveFamilyCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
