using AutoMapper;
using InnNou.Application.Responses.Common;

namespace InnNou.Application
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<InnNou.Domain.Dtos.UserDto, InnNou.Application.Requests.CreateUserCommandRequest>().ReverseMap();
            CreateMap<InnNou.Domain.Dtos.UserDto, InnNou.Application.Responses.CreateUserCommandResponse>().ReverseMap();
            CreateMap<InnNou.Domain.Dtos.UserDto, User>().ReverseMap();
            CreateMap<InnNou.Domain.Dtos.UserDto, InnNou.Application.Responses.EditUserCommandResponse>().ReverseMap();
            CreateMap<InnNou.Domain.Dtos.UserDto, InnNou.Application.Requests.EditUserCommandRequest>().ReverseMap();

            CreateMap<InnNou.Domain.Dtos.HotelDto, Hotel>().ReverseMap();

            CreateMap<InnNou.Domain.Dtos.RoleDto, Role>().ReverseMap();
        }
    }
}
