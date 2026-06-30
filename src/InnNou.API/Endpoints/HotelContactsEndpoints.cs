using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints
{
    public class HotelContactsEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/hotelContacts")
                           .RequireAuthorization();

            group.MapPost("/getContactsByHotelToken", HandleGetContactsByHotelToken)
                .Produces<ApiResponse<GetHotelContactsByHotelTokenQueryResponse>>(200);

            group.MapPost("/getContactByToken", HandleGetContactByToken)
                .Produces<ApiResponse<GetHotelContactByTokenQueryResponse>>(200);

            group.MapPost("/createContact", HandleCreateContact)
                .Produces<ApiResponse<CreateHotelContactCommandResponse>>(201);

            group.MapPost("/editContact", HandleEditContact)
                .Produces<ApiResponse<EditHotelContactCommandResponse>>(200);

            group.MapPost("/deleteContact", HandleDeleteContact)
                .Produces<ApiResponse<DeleteHotelContactCommandResponse>>(200);
        }

        private static async Task<ApiResponse<GetHotelContactsByHotelTokenQueryResponse>> HandleGetContactsByHotelToken(
            [FromBody] GetHotelContactsByHotelTokenQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<GetHotelContactsByHotelTokenQueryResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<GetHotelContactByTokenQueryResponse>> HandleGetContactByToken(
            [FromBody] GetHotelContactByTokenQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<GetHotelContactByTokenQueryResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<CreateHotelContactCommandResponse>> HandleCreateContact(
            [FromBody] CreateHotelContactCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<CreateHotelContactCommandResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<EditHotelContactCommandResponse>> HandleEditContact(
            [FromBody] EditHotelContactCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<EditHotelContactCommandResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<DeleteHotelContactCommandResponse>> HandleDeleteContact(
            [FromBody] DeleteHotelContactCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<DeleteHotelContactCommandResponse>.FailureResponse(result.Errors);
            return result;
        }
    }
}
