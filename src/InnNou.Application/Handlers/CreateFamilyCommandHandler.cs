using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateFamilyCommandHandler(IFamilyService familyService, IMapper mapper)
        : IRequestHandler<CreateFamilyCommandRequest, ApiResponse<CreateFamilyCommandResponse>>
    {
        public async Task<ApiResponse<CreateFamilyCommandResponse>> Handle(CreateFamilyCommandRequest request, CancellationToken cancellationToken)
        {
            if (await familyService.ExistsByCodeAsync(request.Code, cancellationToken))
                return ApiResponse<CreateFamilyCommandResponse>.FailureResponse("FAMILY_CODE_EXISTS", "A family with this code already exists.", 409);

            var dto = new FamilyDto { Code = request.Code };
            var result = await familyService.CreateAsync(dto, cancellationToken);
            if (result is null)
                return ApiResponse<CreateFamilyCommandResponse>.FailureResponse("FAMILY_CREATE_FAILED", "Family could not be created.", 500);

            var response = new CreateFamilyCommandResponse { Family = mapper.Map<Responses.Common.Family>(result) };
            return ApiResponse<CreateFamilyCommandResponse>.SuccessResponse(response, 201);
        }
    }
}
