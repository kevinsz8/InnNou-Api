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

        public async Task<UserDto?> CreateUserAsync(UserDto userDto, IRequestContext context, CancellationToken cancellationToken)
        {
            // validate role
            var role = await _dbContext.Roles
                .FirstOrDefaultAsync(r => r.RoleId == userDto.RoleId, cancellationToken);

            if (role == null)
                throw new Exception("Invalid role");

            // Not able to create someone with higher level
            if (role.Level > context.RoleLevel)
                throw new UnauthorizedAccessException("Cannot assign higher role");

            // HOTEL RULES
            if (context.RoleLevel < 100) // no super admin
            {
                if (!context.HotelId.HasValue)
                    throw new UnauthorizedAccessException("Invalid hotel context");

                var allowedHotelIds = await GetAllowedHotelIds(context.HotelId.Value, cancellationToken);

                if (!userDto.HotelId.HasValue || !allowedHotelIds.Contains(userDto.HotelId.Value))
                    throw new UnauthorizedAccessException("Invalid hotel assignment");
            }

            var user = _mapper.Map<User>(userDto);

            user.UserToken = Guid.NewGuid();
            user.CreatedBy = context.ActorUserToken.ToString();
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
            var query =
                from u in _dbContext.Users.AsNoTracking()
                join r in _dbContext.Roles.AsNoTracking() on u.RoleId equals r.RoleId
                where u.IsActive && r.IsActive
                select new
                {
                    User = u,
                    Role = r
                };

            // ROLE FILTER
            query = query.Where(u => u.Role.Level <= context.RoleLevel);

            // MULTI-TENANT
            if (context.RoleLevel < 100 && context.HotelId.HasValue)
            {
                var allowedHotelIds = await GetAllowedHotelIds(context.HotelId.Value, cancellationToken);

                query = query.Where(x =>
                    x.User.HotelId.HasValue &&
                    allowedHotelIds.Contains(x.User.HotelId.Value)
                );
            }

            // USER only sees his account
            if (context.RoleLevel <= 10)
            {
                query = query.Where(x => x.User.UserToken == context.EffectiveUserToken);
            }

            // SEARCH
            if (!string.IsNullOrWhiteSpace(searchField) && !string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.Trim().ToLower();

                query = searchField.ToLower() switch
                {
                    "email" => query.Where(x => x.User.Email.ToLower().Contains(searchText)),
                    "firstname" => query.Where(x => x.User.FirstName.ToLower().Contains(searchText)),
                    "lastname" => query.Where(x => x.User.LastName.ToLower().Contains(searchText)),
                    "username" => query.Where(x => x.User.UserName.ToLower().Contains(searchText)),
                    _ => query
                };
            }

            var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
            var safePageSize = pageSize < 1 ? 10 : pageSize;
            var offset = (safePageNumber - 1) * safePageSize;

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderBy(x => x.User.UserId)
                .Skip(offset)
                .Take(safePageSize)
                .Select(x => x.User)
                .ToListAsync(cancellationToken);

            return new PagedResult<UserDto>
            {
                Items = _mapper.Map<List<UserDto>>(users),
                TotalCount = totalCount,
                PageNumber = safePageNumber,
                PageSize = safePageSize
            };
        }

        // EDIT USER 
        public async Task<UserDto?> EditUserAsync(UserDto request, IRequestContext context, CancellationToken cancellationToken)
        {
            var result = await (
                from u in _dbContext.Users
                join r in _dbContext.Roles on u.RoleId equals r.RoleId
                where u.UserToken == request.UserToken
                select new
                {
                    User = u,
                    Role = r
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (result == null)
                return null;

            var user = result.User;
            var role = result.Role;

            if (role.Level > context.RoleLevel)
                throw new UnauthorizedAccessException("Cannot edit higher role");

            // MULTI-TENANT
            if (context.RoleLevel < 100 && context.HotelId.HasValue)
            {
                var allowedHotelIds = await GetAllowedHotelIds(context.HotelId.Value, cancellationToken);

                if (!user.HotelId.HasValue || !allowedHotelIds.Contains(user.HotelId.Value))
                    throw new UnauthorizedAccessException("Cannot edit user from another hotel");
            }

            if (request.RoleId != 0 && request.RoleId != user.RoleId)
            {
                var newRole = await _dbContext.Roles
                    .FirstOrDefaultAsync(r => r.RoleId == request.RoleId, cancellationToken);

                if (newRole == null)
                    throw new Exception("Invalid role");

                if (newRole.Level > context.RoleLevel)
                    throw new UnauthorizedAccessException("Cannot assign higher role");

                user.RoleId = newRole.RoleId;
            }

            // UPDATE
            if (!string.IsNullOrWhiteSpace(request.Email)) user.Email = request.Email;
            if (!string.IsNullOrWhiteSpace(request.FirstName)) user.FirstName = request.FirstName;
            if (!string.IsNullOrWhiteSpace(request.LastName)) user.LastName = request.LastName;
            if (!string.IsNullOrWhiteSpace(request.UserName)) user.UserName = request.UserName;

            if (!string.IsNullOrWhiteSpace(request.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            user.LastUpdatedUtc = DateTime.UtcNow;
            user.LastUpdatedBy = context.ActorUserToken.ToString();

            await _dbContext.SaveChangesAsync(cancellationToken);

            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> DeleteUserAsync(Guid userToken,  IRequestContext context,   CancellationToken cancellationToken)
        {
            var result = await (
                from u in _dbContext.Users
                join r in _dbContext.Roles on u.RoleId equals r.RoleId
                where u.UserToken == userToken
                select new
                {
                    User = u,
                    Role = r
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (result == null)
                return false;

            var user = result.User;
            var role = result.Role;

            if (role.Level > context.RoleLevel)
                throw new UnauthorizedAccessException("Cannot delete higher role");

            // MULTI-TENANT
            if (context.RoleLevel < 100 && context.HotelId.HasValue)
            {
                var allowedHotelIds = await GetAllowedHotelIds(context.HotelId.Value, cancellationToken);

                if (!user.HotelId.HasValue || !allowedHotelIds.Contains(user.HotelId.Value))
                    throw new UnauthorizedAccessException("Cannot delete user from another hotel");
            }

            user.IsActive = false;
            user.LastUpdatedUtc = DateTime.UtcNow;
            user.LastUpdatedBy = context.ActorUserToken.ToString();

            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<bool> IsUserExists(string email, CancellationToken cancellationToken)
        {
            return await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email == email, cancellationToken);
        }

        private async Task<List<int>> GetAllowedHotelIds(int rootHotelId, CancellationToken ct)
        {
            var allHotels = await _dbContext.Hotels
                .AsNoTracking()
                .ToListAsync(ct);

            var result = new List<int>();

            void Traverse(int parentId)
            {
                result.Add(parentId);

                var children = allHotels
                    .Where(h => h.ParentHotelId == parentId)
                    .Select(h => h.HotelId);

                foreach (var child in children)
                {
                    Traverse(child);
                }
            }

            Traverse(rootHotelId);

            return result;
        }
    }
}