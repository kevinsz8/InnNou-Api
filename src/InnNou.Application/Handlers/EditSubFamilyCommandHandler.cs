using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditSubFamilyCommandHandler(ISubFamilyService subFamilyService, IMapper mapper)
        : IRequestHandler<EditSubFamilyCommandRequest, ApiResponse<EditSubFamilyCommandResponse>>
    {
        public async Task<ApiResponse<EditSubFamilyCommandResponse>> Handle(EditSubFamilyCommandRequest request, CancellationToken cancellationToken)
        {
            var dto = new SubFamilyDto { SubFamilyToken = request.SubFamilyToken, Code = request.Code };
            var result = await subFamilyService.EditAsync(dto, cancellationToken);
            if (result is null)
                return ApiResponse<EditSubFamilyCommandResponse>.FailureResponse(ErrorCodes.SubFamilyNotFound, "Sub-family not found.", 404);

            var response = new EditSubFamilyCommandResponse { SubFamily = mapper.Map<Responses.Common.SubFamily>(result) };
            return ApiResponse<EditSubFamilyCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
