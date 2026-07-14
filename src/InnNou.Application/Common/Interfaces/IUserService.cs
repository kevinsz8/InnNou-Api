using InnNou.Application.Common;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Persistence
{
    public interface IUserService
    {
        Task<UserDto?> CreateUserAsync(UserDto userDto, IRequestContext context, CancellationToken cancellationToken);
        Task<PagedResult<UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? searchField, string? searchText, bool includeInactive, IRequestContext context, CancellationToken cancellationToken);
        Task<UserDto?> GetUserByTokenAsync(Guid userToken, IRequestContext context, CancellationToken cancellationToken);
        Task<UserDto?> EditUserAsync(UserDto request, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteUserAsync(Guid userToken, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> IsUserExists(string email, CancellationToken cancellationToken);
        Task<BulkImportResultDto> BulkImportUsersAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken);
        Task<(byte[] FileBytes, string FileName)> ExportUsersAsync(string? searchField, string? searchText, bool includeInactive, string? language, IRequestContext context, CancellationToken cancellationToken);
        Task<(byte[] FileBytes, string FileName)> GenerateUserImportTemplateAsync(string? language, IRequestContext context, CancellationToken cancellationToken);
    }
}
