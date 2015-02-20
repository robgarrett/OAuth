using System;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataHandler;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Infrastructure;
using Owin;

namespace OAuth.AuthorizationServer.Middleware
{
    /// <summary>
    /// Factory to create a new handler to insert into the OWIN pipeline.
    /// </summary>
    public class DummyAuthenticationMiddleware : AuthenticationMiddleware<DummyAuthenticationOptions>
    {
        #region Construction

        public DummyAuthenticationMiddleware(OwinMiddleware next, IAppBuilder app, DummyAuthenticationOptions options) : 
            base(next, options)
        {
            if (String.IsNullOrEmpty(options.SignInAsAuthenticationType))
                options.SignInAsAuthenticationType = app.GetDefaultSignInAsAuthenticationType();
            if (null != options.StateDataFormat) return;
            var dataProtector = app.CreateDataProtector(
                typeof (DummyAuthenticationMiddleware).FullName, options.AuthenticationType);
            options.StateDataFormat = new PropertiesDataFormat(dataProtector);
        }

        #endregion Construction

        #region Methods

        /// <summary>
        /// Create a new handler to insert into the OWIN pipeline.
        /// </summary>
        protected override AuthenticationHandler<DummyAuthenticationOptions> CreateHandler()
        {
            return new DummyAuthenticationHandler();
        }

        #endregion Methods
    }
}