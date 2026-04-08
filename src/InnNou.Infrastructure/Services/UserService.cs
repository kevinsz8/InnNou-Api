using AutoMapper;
using InnNou.Application.Common;
using InnNou.Application.Persistence;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Repositories.DbContexts;
using InnNou.Infrastructure.Repositories.DbEntities;
using Microsoft.EntityFrameworkCore;

namespace InnNou.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly InnNouDbContext _dbContext;
        private readonly IMapper _mapper;
        public UserService(InnNouDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }
        public async Task<UserDto?> CreateUserAsync(UserDto userDto, CancellationToken cancellationToken)
        {
            var user = _mapper.Map<User>(userDto);

            user.UserToken = Guid.NewGuid();
            user.CreatedBy = "System";
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password);
            user.IsActive = true;

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return _mapper.Map<UserDto>(user);
        }

        public async Task<PagedResult<UserDto>> GetUsersAsync(
        int pageNumber,
        int pageSize,
        string? searchField,
        string? searchText,
        IRequestContext context,
        CancellationToken cancellationToken)
        {
            var query = _dbContext.Users
                .AsNoTracking()
                .Where(x => x.IsActive)
                .AsQueryable();

            // MULTI-TENANT FILTER
            if (context.HotelId.HasValue)
            {
                var allowedHotelIds = await GetAllowedHotelIds(context.HotelId.Value, cancellationToken);

                query = query.Where(u => u.HotelId.HasValue && allowedHotelIds.Contains(u.HotelId.Value));
            }

            // SEARCH
            if (!string.IsNullOrWhiteSpace(searchField) && !string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.Trim().ToLower();

                query = searchField.ToLower() switch
                {
                    "email" => query.Where(u => u.Email.ToLower().Contains(searchText)),
                    "firstname" => query.Where(u => u.FirstName.ToLower().Contains(searchText)),
                    "lastname" => query.Where(u => u.LastName.ToLower().Contains(searchText)),
                    "username" => query.Where(u => u.UserName.ToLower().Contains(searchText)),
                    _ => query
                };
            }

            var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
            var safePageSize = pageSize < 1 ? 10 : pageSize;
            var offset = (safePageNumber - 1) * safePageSize;

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderBy(u => u.UserId)
                .Skip(offset)
                .Take(safePageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<UserDto>
            {
                Items = _mapper.Map<List<UserDto>>(users),
                TotalCount = totalCount,
                PageNumber = safePageNumber,
                PageSize = safePageSize
            };
        }
        public async Task<UserDto?> EditUserAsync(UserDto request, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);
            if (user == null)
                return null;
            if (!string.IsNullOrWhiteSpace(request.Email)) user.Email = request.Email;
            if (!string.IsNullOrWhiteSpace(request.FirstName)) user.FirstName = request.FirstName;
            if (!string.IsNullOrWhiteSpace(request.LastName)) user.LastName = request.LastName;
            if (!string.IsNullOrWhiteSpace(request.UserName)) user.UserName = request.UserName;
            if (!string.IsNullOrWhiteSpace(request.Password)) user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> DeleteUserAsync(int userId, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
            if (user == null)
                return false;

            user.IsActive = false;
            //_dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> IsUserExists(string email, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user != null)
                return true;

            return false;
        }

        private async Task<List<int>> GetAllowedHotelIds(int rootHotelId, CancellationToken ct)
        {
            var result = new List<int> { rootHotelId };

            var children = await _dbContext.Hotels
                .Where(h => h.ParentHotelId == rootHotelId)
                .Select(h => h.HotelId)
                .ToListAsync(ct);

            foreach (var child in children)
            {
                result.AddRange(await GetAllowedHotelIds(child, ct));
            }

            return result;
        }
    }
}
