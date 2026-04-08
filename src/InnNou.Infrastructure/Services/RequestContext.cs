using InnNou.Application.Common;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace InnNou.Infrastructure.Services
{
    public class RequestContext : IRequestContext
    {
        public Guid ActorUserToken { get; private set; }
        public Guid EffectiveUserToken { get; private set; }
        public int? HotelId { get; private set; }

        public bool IsAuthenticated { get; private set; }
        public bool IsImpersonating => ActorUserToken != EffectiveUserToken;

        public int RoleLevel { get; private set; }

        public RequestContext(IHttpContextAccessor httpContextAccessor)
        {
            var httpContext = httpContextAccessor.HttpContext;

            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                IsAuthenticated = false;
                return;
            }

            var user = httpContext.User;
            IsAuthenticated = true;


            var actorClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? user.FindFirst("sub")?.Value;

            if (!Guid.TryParse(actorClaim, out var actorToken))
                throw new UnauthorizedAccessException("Invalid or missing user token");

            ActorUserToken = actorToken;


            var impersonatedClaim = user.FindFirst("impersonatedUserToken")?.Value;

            if (!string.IsNullOrWhiteSpace(impersonatedClaim) &&
                Guid.TryParse(impersonatedClaim, out var impersonatedToken))
            {
                EffectiveUserToken = impersonatedToken;
            }
            else
            {
                EffectiveUserToken = ActorUserToken;
            }

            var hotelClaim = user.FindFirst("hotelId")?.Value;

            if (!string.IsNullOrWhiteSpace(hotelClaim) &&
                int.TryParse(hotelClaim, out var hotelId))
            {
                HotelId = hotelId;
            }

            var roleLevelClaim = user.FindFirst("roleLevel")?.Value;

            if (!string.IsNullOrWhiteSpace(roleLevelClaim) &&
                int.TryParse(roleLevelClaim, out var roleLevel))
            {
                RoleLevel = roleLevel;
            }
            else
            {

                RoleLevel = 0;
            }
        }
    }
}