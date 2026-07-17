using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints
{
    public class OrganizationContactsEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/organizationContacts")
                           .RequireAuthorization();

            group.MapPost("/getContactsByOrganizationToken", HandleGetContactsByOrganizationToken)
                .Produces<ApiResponse<GetOrganizationContactsByOrganizationTokenQueryResponse>>(200);

            group.MapPost("/getContactByToken", HandleGetContactByToken)
                .Produces<ApiResponse<GetOrganizationContactByTokenQueryResponse>>(200);

            group.MapPost("/createContact", HandleCreateContact)
                .Produces<ApiResponse<CreateOrganizationContactCommandResponse>>(201);

            group.MapPost("/editContact", HandleEditContact)
                .Produces<ApiResponse<EditOrganizationContactCommandResponse>>(200);

            group.MapPost("/deleteContact", HandleDeleteContact)
                .Produces<ApiResponse<DeleteOrganizationContactCommandResponse>>(200);
        }

        private static async Task<IResult> HandleGetContactsByOrganizationToken(
            [FromBody] GetOrganizationContactsByOrganizationTokenQueryRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var result = await sender.Send(request, ct);
            return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
        }

        private static async Task<IResult> HandleGetContactByToken(
            [FromBody] GetOrganizationContactByTokenQueryRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var result = await sender.Send(request, ct);
            return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
        }

        private static async Task<IResult> HandleCreateContact(
            [FromBody] CreateOrganizationContactCommandRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var result = await sender.Send(request, ct);
            return result.Success ? Results.Created("/organizationContacts", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
        }

        private static async Task<IResult> HandleEditContact(
            [FromBody] EditOrganizationContactCommandRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var result = await sender.Send(request, ct);
            return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
        }

        private static async Task<IResult> HandleDeleteContact(
            [FromBody] DeleteOrganizationContactCommandRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var result = await sender.Send(request, ct);
            return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
        }
    }
}
