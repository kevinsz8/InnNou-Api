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

        private static async Task<ApiResponse<GetOrganizationsQueryResponse>> HandleGetOrganizations(
            [FromBody] GetOrganizationsQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<GetOrganizationsQueryResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<GetOrganizationByTokenQueryResponse>> HandleGetOrganizationByToken(
            [FromBody] GetOrganizationByTokenQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<GetOrganizationByTokenQueryResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<CreateOrganizationCommandResponse>> HandleCreateOrganization(
            [FromBody] CreateOrganizationCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<CreateOrganizationCommandResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<EditOrganizationCommandResponse>> HandleEditOrganization(
            [FromBody] EditOrganizationCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<EditOrganizationCommandResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<DeleteOrganizationCommandResponse>> HandleDeleteOrganization(
            [FromBody] DeleteOrganizationCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<DeleteOrganizationCommandResponse>.FailureResponse(result.Errors);
            return result;
        }
    }
}
