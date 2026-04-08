using AutoMapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Repositories.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace InnNou.Infrastructure.Services
{
    public class HotelService : IHotelService
    {
        private readonly InnNouDbContext _dbContext;
        private readonly IMapper _mapper;

        public HotelService(InnNouDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<PagedResult<HotelDto>> GetHotelsAsync(int pageNumber, int pageSize, string? searchField, string? searchText, IRequestContext context, CancellationToken cancellationToken)
        {
            var query = _dbContext.Hotels
                .AsNoTracking()
                .Where(x => x.IsActive)
                .AsQueryable();

            var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
            var safePageSize = pageSize < 1 ? 10 : pageSize;
            var offset = (safePageNumber - 1) * safePageSize;

            var totalCount = await query.CountAsync(cancellationToken);

            var hotels = await query
                .OrderBy(u => u.HotelId)
                .Skip(offset)
                .Take(safePageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<HotelDto>
            {
                Items = _mapper.Map<List<HotelDto>>(hotels),
                TotalCount = totalCount,
                PageNumber = safePageNumber,
                PageSize = safePageSize
            };
        }
    }
}
