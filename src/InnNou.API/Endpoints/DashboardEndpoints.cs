using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class DashboardEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dashboard").RequireAuthorization();

        group.MapPost("/getSummary", HandleGetSummary).Produces<ApiResponse<GetDashboardSummaryQueryResponse>>(200);
    }

    private static async Task<IResult> HandleGetSummary([FromBody] GetDashboardSummaryQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
