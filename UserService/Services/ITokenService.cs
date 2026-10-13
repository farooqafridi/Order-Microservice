using UserService.Models;

namespace UserService.Services
{
    public interface ITokenService
    {
        Task<string> CreateAccessTokenAsync(ApplicationUser user);
    }
}
