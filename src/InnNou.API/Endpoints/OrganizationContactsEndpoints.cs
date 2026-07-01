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

        private static async Task<ApiResponse<GetOrganizationContactsByOrganizationTokenQueryResponse>> HandleGetContactsByOrganizationToken(
            [FromBody] GetOrganizationContactsByOrganizationTokenQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<GetOrganizationContactsByOrganizationTokenQueryResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<GetOrganizationContactByTokenQueryResponse>> HandleGetContactByToken(
            [FromBody] GetOrganizationContactByTokenQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<GetOrganizationContactByTokenQueryResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<CreateOrganizationContactCommandResponse>> HandleCreateContact(
            [FromBody] CreateOrganizationContactCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<CreateOrganizationContactCommandResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<EditOrganizationContactCommandResponse>> HandleEditContact(
            [FromBody] EditOrganizationContactCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<EditOrganizationContactCommandResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<DeleteOrganizationContactCommandResponse>> HandleDeleteContact(
            [FromBody] DeleteOrganizationContactCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<DeleteOrganizationContactCommandResponse>.FailureResponse(result.Errors);
            return result;
        }
    }
}
