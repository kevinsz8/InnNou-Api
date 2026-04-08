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
    }
}
