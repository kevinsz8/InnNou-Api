using AutoMapper;
using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Repositories.DbEntities;

namespace InnNou.Infrastructure.Mappers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Tenant, TenantDto>().ReverseMap();
            CreateMap<User, UserDto>().ReverseMap();
        }
    }
}
