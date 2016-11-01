using System.Net;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using MyAnimeList.API.ServicesContracts;

namespace MyAnimeList.API.Services
{
    public class AuthorizationService : BaseService, IAuthorizationService
    {
        private const string CredentialsUrl = "https://myanimelist.net/api/account/verify_credentials.xml";

        public AuthorizationService(string userAgent)
            : base(userAgent)
        {
        }

        public async Task<bool> VerifyCredentialsAsync(string login, string password)
        {
            await GetAsync(CredentialsUrl, new PasswordCredential("pass", login, password));
            return true;
        }

    }
}