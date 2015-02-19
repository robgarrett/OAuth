using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;

namespace OAuth.AuthorizationServer.Models
{
    /// <summary>
    /// ASP.NET Identity User Manager for OAUTH Authorization Server.
    /// </summary>
    public class OAuthUserManager : UserManager<OAuthUser>
    {
        #region Construction

        public OAuthUserManager(IUserStore<OAuthUser> store) : base(store)
        {
        }

        #endregion Construction

        #region Methods

        public static OAuthUserManager Create(IdentityFactoryOptions<OAuthUserManager> options, IOwinContext context)
        {
            // TODO: Add policy for passwords, two-factor auth etc to the manager here.
            return new OAuthUserManager(new UserStore<OAuthUser>(context.Get<OAuthDbContext>()));
        }

        #endregion Methods
    }
}