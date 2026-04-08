namespace InnNou.Application.Common
{
    public interface IRequestContext
    {
        Guid ActorUserToken { get; }
        Guid EffectiveUserToken { get; }
        int? HotelId { get; }
        int RoleLevel { get; }
        bool IsAuthenticated { get; }
        bool IsImpersonating { get; }
    }
}
