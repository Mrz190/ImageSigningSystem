using API.Dto;
using API.Entity;
using AutoMapper;

namespace API.Mapping
{
    public class BaseMapperProfile : Profile
    {
        public BaseMapperProfile()
        {
            CreateMap<RegDto, AppUser>();
            CreateMap<ImageDto, SignedImage>().ReverseMap();
        }
    }
}
