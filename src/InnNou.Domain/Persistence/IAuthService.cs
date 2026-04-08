using InnNou.Domain.Models;

namespace InnNou.Domain.Persistence
{
    public interface IAuthService
    {
        Task<Login?> LoginAsync(string email, string password, CancellationToken cancellationToken);
        Task<Login?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
        Task<Login?> ImpersonateAsync(Guid actorUserToken, Guid targetUserToken, CancellationToken cancellationToken);
    }
}
