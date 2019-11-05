using System;
using System.Threading.Tasks;

namespace spoauth
{
    /*
        Ported from https://github.com/s-KaiNet/node-sp-auth.
     */
    class Program
    {
        class Creds : IUserCredentials
        {
            public string username { get; set; }
            public string password { get; set; }
            public bool? online { get; set; }

            public Creds(string username, string password, bool? online = null)
            {
                this.username = username;
                this.password = password;
                this.online = online;
            }

            public IUserCredentials Clone(IUserCredentials creds)
            {
                return new Creds(username, password, online);
            }
        }
        static async Task Main(string[] args)
        {
            var resolver = AuthResolverFactory.resolve("https://rdgdemo.sharepoint.com", new Creds("admin@rdgdemo.onmicrosoft.com", "password"));
            await resolver.getAuthAsync();
        }
    }
}
