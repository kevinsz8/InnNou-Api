namespace InnNou.Application.Common
{
    public interface IRequestContext
    {
        Guid ActorUserToken { get; }
        Guid EffectiveUserToken { get; }
        int? OrganizationId { get; }
        string? OrganizationTypeCode { get; }
        int? SupplierId { get; }
        int RoleLevel { get; }
        int ActorRoleLevel { get; }
        int? ActorOrganizationId { get; }
        bool IsAuthenticated { get; }
        bool IsImpersonating { get; }
    }
}
