using AutoMapper;
using InnNou.Domain.Dtos;
using InnNou.Domain.Persistence;
using InnNou.Infrastructure.Repositories.DbContexts;
using InnNou.Infrastructure.Repositories.DbEntities;
using Microsoft.EntityFrameworkCore;

namespace InnNou.Infrastructure.Services
{
    public class TenantService : ITenantService
    {
        private readonly InnNouDbContext _dbContext;
        private readonly IMapper _mapper;

        public TenantService(InnNouDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<TenantDto?> CreateTenantAsync(string name, string slug, CancellationToken cancellationToken)
        {
            var tenant = _mapper.Map<Tenant>(new TenantDto { Name = name, Slug = slug });
            _dbContext.Tenants.Add(tenant);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return _mapper.Map<TenantDto>(tenant);
        }

        public async Task<List<TenantDto>> GetTenantsAsync(CancellationToken cancellationToken)
        {
            var tenants = await _dbContext.Tenants.ToListAsync(cancellationToken);
            return _mapper.Map<List<TenantDto>>(tenants);
        }

        public async Task<TenantDto?> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            var tenant = await _dbContext.Tenants.FindAsync(new object[] { tenantId }, cancellationToken);
            return tenant == null ? null : _mapper.Map<TenantDto>(tenant);
        }
    }
}
