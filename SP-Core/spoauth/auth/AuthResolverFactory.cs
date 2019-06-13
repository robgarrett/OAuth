
namespace spoauth
{
    public class AuthResolverFactory
    {
        public static IAuthResolver resolve(string siteUrl, IUserCredentials creds)
        {
            return new OnlineUserCredentials(siteUrl, creds);
        }
    }
}