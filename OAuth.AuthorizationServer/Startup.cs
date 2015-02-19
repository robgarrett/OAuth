using Owin;
using Microsoft.Owin;

[assembly: OwinStartup(typeof(OAuth.AuthorizationServer.Startup))]
namespace OAuth.AuthorizationServer
{
    /// <summary>
    /// Startup class called by Katana.
    /// </summary>
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configure authentication.
            ConfigureAuth(app);
        }
    }
}
