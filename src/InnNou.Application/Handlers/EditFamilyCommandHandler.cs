using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditFamilyCommandHandler(IFamilyService familyService, IMapper mapper)
        : IRequestHandler<EditFamilyCommandRequest, ApiResponse<EditFamilyCommandResponse>>
    {
        public async Task<ApiResponse<EditFamilyCommandResponse>> Handle(EditFamilyCommandRequest request, CancellationToken cancellationToken)
        {
            var dto = new FamilyDto { FamilyToken = request.FamilyToken, Code = request.Code };
            var result = await familyService.EditAsync(dto, cancellationToken);
            if (result is null)
                return ApiResponse<EditFamilyCommandResponse>.FailureResponse(ErrorCodes.FamilyNotFound, "Family not found.", 404);

            var response = new EditFamilyCommandResponse { Family = mapper.Map<Responses.Common.Family>(result) };
            return ApiResponse<EditFamilyCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
