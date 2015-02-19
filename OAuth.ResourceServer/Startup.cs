using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(OAuth.ResourceServer.Startup))]
namespace OAuth.ResourceServer
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseOAuthBearerAuthentication(new Microsoft.Owin.Security.OAuth.OAuthBearerAuthenticationOptions()); 
        }
    }
}