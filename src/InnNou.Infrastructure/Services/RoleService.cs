using AutoMapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Repositories.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace InnNou.Infrastructure.Services
{
    public class RoleService : IRoleService
    {
        private readonly InnNouDbContext _dbContext;
        private readonly IMapper _mapper;

        public RoleService(InnNouDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<PagedResult<RoleDto>> GetRolesAsync(int pageNumber, int pageSize, string? searchField, string? searchText, IRequestContext context, CancellationToken cancellationToken)
        {
            var query = _dbContext.Roles
                .AsNoTracking()
                .Where(x => x.IsActive)
                .AsQueryable();

            query = query.Where(r => r.Level <= context.RoleLevel);

            var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
            var safePageSize = pageSize < 1 ? 10 : pageSize;
            var offset = (safePageNumber - 1) * safePageSize;

            var totalCount = await query.CountAsync(cancellationToken);

            var Roles = await query
                .OrderByDescending(r => r.Level)
                .Skip(offset)
                .Take(safePageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<RoleDto>
            {
                Items = _mapper.Map<List<RoleDto>>(Roles),
                TotalCount = totalCount,
                PageNumber = safePageNumber,
                PageSize = safePageSize
            };
        }
    }
}
