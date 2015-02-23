using Microsoft.Owin;
using Microsoft.Owin.Security.Infrastructure;
using Owin;

namespace OAuth.AuthorizationServer.Middleware
{
    /// <summary>
    /// Factory to create a new handler to insert into the OWIN pipeline.
    /// </summary>
    public class BasicAuthenticationMiddleware : AuthenticationMiddleware<BasicAuthenticationOptions>
    {
        #region Construction

        public BasicAuthenticationMiddleware(OwinMiddleware next, IAppBuilder app, BasicAuthenticationOptions options) :
            base(next, options)
        {
        }

        #endregion Construction

        #region Methods

        /// <summary>
        /// Create a new handler to insert into the OWIN pipeline.
        /// </summary>
        protected override AuthenticationHandler<BasicAuthenticationOptions> CreateHandler()
        {
            return new BasicAuthenticationHandler();
        }

        #endregion Methods
    }
}