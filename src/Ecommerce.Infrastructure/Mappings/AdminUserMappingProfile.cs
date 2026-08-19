using AutoMapper;
using Ecommerce.Application.DTOs;
using Ecommerce.Infrastructure.Identity;

namespace Ecommerce.Infrastructure.Mappings
{
    public class AdminUserMappingProfile : Profile
    {
        public AdminUserMappingProfile()
        {
            CreateMap<ApplicationUser, AdminUserDto>();
        }
    }
}