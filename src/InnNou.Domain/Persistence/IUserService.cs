using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Domain.Persistence
{
    public interface IUserService
    {
        Task<UserDto?> CreateUserAsync(UserDto user, CancellationToken cancellationToken);
        Task<PagedResult<UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? searchField, string? searchText, CancellationToken cancellationToken);
        Task<UserDto?> EditUserAsync(UserDto request, CancellationToken cancellationToken);
        Task<bool> DeleteUserAsync(int userId, CancellationToken cancellationToken);
        Task<bool> IsUserExists(string email, CancellationToken cancellationToken);
    }
}
