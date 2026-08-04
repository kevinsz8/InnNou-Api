using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace InnNou.API.Hubs;

// One group per resolved EffectiveUserToken (not ActorUserToken) — a SuperAdmin impersonating a
// user sees that user's own live notifications while impersonating, same identity IRequestContext
// resolves everywhere else on the backend. Reads Context.User directly (SignalR guarantees this
// is set from the connection's own authenticated principal for its whole lifetime) rather than
// going through IRequestContext/IHttpContextAccessor — safer for a long-lived connection than
// relying on the ambient-HttpContext pattern IRequestContext uses for a normal per-request scope.
// Claim names/resolution mirror InnNou.Infrastructure.Services.RequestContext exactly.
[Authorize]
public class NotificationsHub : Hub
{
    public static string GroupNameForUser(Guid userToken) => $"user:{userToken}";

    private static Guid? ResolveEffectiveUserToken(ClaimsPrincipal user)
    {
        var actorClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        if (!Guid.TryParse(actorClaim, out var actorToken))
            return null;

        var impersonatedClaim = user.FindFirst("impersonatedUserToken")?.Value;
        if (!string.IsNullOrWhiteSpace(impersonatedClaim) && Guid.TryParse(impersonatedClaim, out var impersonatedToken))
            return impersonatedToken;

        return actorToken;
    }

    public override async Task OnConnectedAsync()
    {
        var effectiveUserToken = ResolveEffectiveUserToken(Context.User!);
        if (effectiveUserToken.HasValue)
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupNameForUser(effectiveUserToken.Value));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var effectiveUserToken = ResolveEffectiveUserToken(Context.User!);
        if (effectiveUserToken.HasValue)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNameForUser(effectiveUserToken.Value));

        await base.OnDisconnectedAsync(exception);
    }
}
