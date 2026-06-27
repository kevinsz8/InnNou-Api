using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateSubFamilyCommandHandler(ISubFamilyService subFamilyService, IFamilyService familyService, IMapper mapper)
        : IRequestHandler<CreateSubFamilyCommandRequest, ApiResponse<CreateSubFamilyCommandResponse>>
    {
        public async Task<ApiResponse<CreateSubFamilyCommandResponse>> Handle(CreateSubFamilyCommandRequest request, CancellationToken cancellationToken)
        {
            var family = await familyService.GetByTokenAsync(request.FamilyToken, cancellationToken);
            if (family is null)
                return ApiResponse<CreateSubFamilyCommandResponse>.FailureResponse("FAMILY_NOT_FOUND", "Family not found.", 404);

            if (await subFamilyService.ExistsByCodeAsync(request.Code, family.FamilyId, cancellationToken))
                return ApiResponse<CreateSubFamilyCommandResponse>.FailureResponse("SUB_FAMILY_CODE_EXISTS", "A sub-family with this code already exists in the family.", 409);

            var dto = new SubFamilyDto { FamilyId = family.FamilyId, Code = request.Code };
            var result = await subFamilyService.CreateAsync(dto, cancellationToken);
            if (result is null)
                return ApiResponse<CreateSubFamilyCommandResponse>.FailureResponse("SUB_FAMILY_CREATE_FAILED", "Sub-family could not be created.", 500);

            var response = new CreateSubFamilyCommandResponse { SubFamily = mapper.Map<Responses.Common.SubFamily>(result) };
            return ApiResponse<CreateSubFamilyCommandResponse>.SuccessResponse(response, 201);
        }
    }
}
