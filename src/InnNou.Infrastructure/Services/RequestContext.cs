using InnNou.Application.Common;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace InnNou.Infrastructure.Services
{
    public class RequestContext : IRequestContext
    {
        public Guid ActorUserToken { get; private set; }
        public Guid EffectiveUserToken { get; private set; }
        public int? OrganizationId { get; private set; }
        public string? OrganizationTypeCode { get; private set; }
        public int? SupplierId { get; private set; }

        public bool IsAuthenticated { get; private set; }
        public bool IsImpersonating => ActorUserToken != EffectiveUserToken;

        public int RoleLevel { get; private set; }
        public int ActorRoleLevel { get; private set; }
        public int? ActorOrganizationId { get; private set; }

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
                throw new ApiException(ErrorCodes.InvalidToken, "Invalid or missing user token", 401);

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

            var organizationClaim = user.FindFirst("organizationId")?.Value;
            if (!string.IsNullOrWhiteSpace(organizationClaim) && int.TryParse(organizationClaim, out var organizationId))
                OrganizationId = organizationId;

            var organizationTypeCodeClaim = user.FindFirst("organizationTypeCode")?.Value;
            if (!string.IsNullOrWhiteSpace(organizationTypeCodeClaim))
                OrganizationTypeCode = organizationTypeCodeClaim;

            var supplierClaim = user.FindFirst("supplierId")?.Value;
            if (!string.IsNullOrWhiteSpace(supplierClaim) && int.TryParse(supplierClaim, out var supplierId))
                SupplierId = supplierId;

            var roleLevelClaim = user.FindFirst("roleLevel")?.Value;
            if (!string.IsNullOrWhiteSpace(roleLevelClaim) && int.TryParse(roleLevelClaim, out var roleLevel))
                RoleLevel = roleLevel;
            else
                RoleLevel = 0;

            var actorRoleLevelClaim = user.FindFirst("actorRoleLevel")?.Value;
            if (!string.IsNullOrWhiteSpace(actorRoleLevelClaim) && int.TryParse(actorRoleLevelClaim, out var actorRoleLevel))
                ActorRoleLevel = actorRoleLevel;
            else
                ActorRoleLevel = RoleLevel;

            var actorOrganizationClaim = user.FindFirst("actorOrganizationId")?.Value;
            if (!string.IsNullOrWhiteSpace(actorOrganizationClaim) && int.TryParse(actorOrganizationClaim, out var actorOrganizationId))
                ActorOrganizationId = actorOrganizationId;
            else
                ActorOrganizationId = OrganizationId;
        }
    }
}
