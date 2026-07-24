using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateInventoryTransferCommandHandler(IInventoryService inventoryService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateInventoryTransferCommandRequest, ApiResponse<CreateInventoryTransferCommandResponse>>
    {
        public async Task<ApiResponse<CreateInventoryTransferCommandResponse>> Handle(CreateInventoryTransferCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.FromWarehouseToken == Guid.Empty || request.ToWarehouseToken == Guid.Empty)
                return ApiResponse<CreateInventoryTransferCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "FromWarehouseToken and ToWarehouseToken are required.", 400);

            if (request.Lines is null || request.Lines.Count == 0)
                return ApiResponse<CreateInventoryTransferCommandResponse>.FailureResponse(ErrorCodes.InventoryTransferEmpty, "At least one line must be transferred.", 400);

            var lines = request.Lines.Select(l => new CreateInventoryTransferLineInputDto
            {
                ArticleToken = l.ArticleToken,
                Quantity = l.Quantity,
                Notes = l.Notes
            }).ToList();

            var result = await inventoryService.CreateTransferAsync(request.FromWarehouseToken, request.ToWarehouseToken, request.Notes, lines, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateInventoryTransferCommandResponse>.FailureResponse(ErrorCodes.InventoryWarehouseNotFound, "Warehouse not found.", 404);

            return ApiResponse<CreateInventoryTransferCommandResponse>.SuccessResponse(new CreateInventoryTransferCommandResponse
            {
                InventoryTransfer = mapper.Map<Responses.Common.InventoryTransfer>(result)
            }, 201);
        }
    }
}
