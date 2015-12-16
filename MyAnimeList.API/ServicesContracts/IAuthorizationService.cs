using System.Threading.Tasks;

namespace MyAnimeList.API.ServicesContracts
{
    public interface IAuthorizationService
    {
        Task<bool> VerifyCredentialsAsync(string login, string password);
    }
}