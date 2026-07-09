using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints
{
    public class OrganizationEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/organizations")
                       .RequireAuthorization();

            group.MapPost("/getOrganizations", HandleGetOrganizations)
                .Produces<ApiResponse<GetOrganizationsQueryResponse>>(200);

            group.MapPost("/getOrganizationByToken", HandleGetOrganizationByToken)
                .Produces<ApiResponse<GetOrganizationByTokenQueryResponse>>(200);

            group.MapPost("/createOrganization", HandleCreateOrganization)
                .Produces<ApiResponse<CreateOrganizationCommandResponse>>(201);

            group.MapPost("/editOrganization", HandleEditOrganization)
                .Produces<ApiResponse<EditOrganizationCommandResponse>>(200);

            group.MapPost("/deleteOrganization", HandleDeleteOrganization)
                .Produces<ApiResponse<DeleteOrganizationCommandResponse>>(200);
        }

        private static async Task<IResult> HandleGetOrganizations(
            [FromBody] GetOrganizationsQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleGetOrganizationByToken(
            [FromBody] GetOrganizationByTokenQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleCreateOrganization(
            [FromBody] CreateOrganizationCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleEditOrganization(
            [FromBody] EditOrganizationCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleDeleteOrganization(
            [FromBody] DeleteOrganizationCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }
    }
}
