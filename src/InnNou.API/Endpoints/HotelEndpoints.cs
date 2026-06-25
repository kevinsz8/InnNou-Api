using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints
{
    public class HotelEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/hotels")
                       .RequireAuthorization();

            group.MapPost("/getHotels", HandleGetHotels)
                .Produces<ApiResponse<GetHotelsQueryResponse>>(200);

            group.MapPost("/getHotelByToken", HandleGetHotelByToken)
                .Produces<ApiResponse<GetHotelByTokenQueryResponse>>(200);

            group.MapPost("/createHotel", HandleCreateHotel)
                .Produces<ApiResponse<CreateHotelCommandResponse>>(201);

            group.MapPost("/editHotel", HandleEditHotel)
                .Produces<ApiResponse<EditHotelCommandResponse>>(200);

            group.MapPost("/deleteHotel", HandleDeleteHotel)
                .Produces<ApiResponse<DeleteHotelCommandResponse>>(200);
        }

        private static async Task<ApiResponse<GetHotelsQueryResponse>> HandleGetHotels(
            [FromBody] GetHotelsQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<GetHotelsQueryResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<GetHotelByTokenQueryResponse>> HandleGetHotelByToken(
            [FromBody] GetHotelByTokenQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<GetHotelByTokenQueryResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<CreateHotelCommandResponse>> HandleCreateHotel(
            [FromBody] CreateHotelCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<CreateHotelCommandResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<EditHotelCommandResponse>> HandleEditHotel(
            [FromBody] EditHotelCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<EditHotelCommandResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<DeleteHotelCommandResponse>> HandleDeleteHotel(
            [FromBody] DeleteHotelCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<DeleteHotelCommandResponse>.FailureResponse(result.Errors);
            return result;
        }
    }
}
