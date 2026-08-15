using System.Threading.Tasks;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces
{
    public interface ITokenService
    {
        Task<string> CreateTokenAsync(ApplicationUserDto user);
    }
}
