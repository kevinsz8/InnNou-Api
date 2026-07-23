using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

// Anonymous single-use email-approval link — see .claude/OrderApprovalModule.md. A SEPARATE
// Carter group from OrdersEndpoints deliberately with NO .RequireAuthorization(): the token
// itself is the entire authorization, there is no session for these calls. Reuses the "auth"
// named rate-limit policy (IP-partitioned, 5/60s) as defense-in-depth for this anonymous,
// security-adjacent surface — same reasoning already applied to login/refresh/impersonate.
public class OrderApprovalEmailEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders/email-approval").RequireRateLimiting("auth");

        group.MapPost("/preview", HandlePreview).Produces<ApiResponse<OrderApprovalEmailPreviewResponse>>(200);
        group.MapPost("/approve", HandleApprove).Produces<ApiResponse<OrderApprovalEmailApproveResultResponse>>(200);
    }

    private static async Task<IResult> HandlePreview([FromBody] PreviewOrderApprovalStepByEmailTokenQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleApprove([FromBody] ApproveOrderApprovalStepByEmailTokenCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
