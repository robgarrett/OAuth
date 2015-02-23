using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Infrastructure;
using Microsoft.Owin.Security.OAuth;
using OAuth.AuthorizationServer.Middleware;
using OAuth.AuthorizationServer.Models;
using OAuth.Constants;
using Owin;

namespace OAuth.AuthorizationServer
{
    /// <summary>
    /// Startup class to configure authentication.
    /// </summary>
    /// <remarks>Supports both OAUTHv2 and Cookie (forms).</remarks>
    public partial class Startup
    {
        private readonly ConcurrentDictionary<string, string> _authenticationCodes =
            new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

        public void ConfigureAuth(IAppBuilder app)
        {
            // Enable ASP.NET User Manager and DBContext objects in the OWIN context.
            app.CreatePerOwinContext(OAuthDbContext.Create);
            app.CreatePerOwinContext<OAuthUserManager>(OAuthUserManager.Create);
            // Enable Application Sign In Cookie 
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = AuthTypes.FormsAuthType,
                AuthenticationMode = AuthenticationMode.Passive,
                LoginPath = new PathString(Paths.LoginPath),
                LogoutPath = new PathString(Paths.LogoutPath),
            });

            // Setup Authorization Server 
            app.UseOAuthAuthorizationServer(new OAuthAuthorizationServerOptions
            {
                AuthorizeEndpointPath = new PathString(Paths.AuthorizePath),
                TokenEndpointPath = new PathString(Paths.TokenPath),
                ApplicationCanDisplayErrors = true,
                #if DEBUG
                AllowInsecureHttp = true,
                #endif
                // Authorization server provider which controls the lifecycle of Authorization Server 
                Provider = new OAuthAuthorizationServerProvider
                {
                    OnValidateClientRedirectUri = ValidateClientRedirectUri,
                    OnValidateClientAuthentication = ValidateClientAuthentication,
                    OnGrantResourceOwnerCredentials = GrantResourceOwnerCredentials,
                    OnGrantClientCredentials = GrantClientCredentuials
                },
                // Authorization code provider which creates and receives authorization code 
                AuthorizationCodeProvider = new AuthenticationTokenProvider
                {
                    OnCreate = CreateAuthenticationCode,
                    OnReceive = ReceiveAuthenticationCode,
                },
                // Refresh token provider which creates and receives referesh token 
                RefreshTokenProvider = new AuthenticationTokenProvider
                {
                    OnCreate = CreateRefreshToken,
                    OnReceive = ReceiveRefreshToken,
                }
            });

            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
            //app.UseDummyAuthentication(new DummyAuthenticationOptions("rgarrett") { AuthenticationType = AuthTypes.DefaultAuthType });
            app.UseWindowsAuthentication(new WindowsAuthenticationOptions(AuthTypes.DefaultAuthType));
        }

        /// <summary>
        /// Lookup the redirect URL for a given client ID.
        /// </summary>
        /// <remarks>
        /// This ensures that our server doesn't just redirect anywhere
        /// and that our client is registered.
        /// </remarks>
        private static Task ValidateClientRedirectUri(OAuthValidateClientRedirectUriContext context)
        {
            context.Validated(Paths.AuthorizeCodeCallBackPath);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Validate client authentication.
        /// </summary>
        /// <remarks>
        /// This method ensures our client has a valid ID and secret.
        /// </remarks>
        private static Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            string clientId;
            string clientSecret;
            if (context.TryGetBasicCredentials(out clientId, out clientSecret) ||
                context.TryGetFormCredentials(out clientId, out clientSecret))
                context.Validated();
            return Task.FromResult(0);
        }

        /// <summary>
        /// Grant Resource Owner credentials.
        /// </summary>
        /// <remarks>
        /// If grant type is Resource Owner, then grant access with a new claim.
        /// </remarks>
        private static Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            var identity = new ClaimsIdentity(new GenericIdentity(context.UserName, OAuthDefaults.AuthenticationType), 
                context.Scope.Select(x => new Claim("urn:oauth:scope", x)));
            context.Validated(identity);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Grant client credentials.
        /// </summary>
        /// <remarks>
        /// If grant type is Client, then grant access to the client with a new claim.
        /// </remarks>
        private static Task GrantClientCredentuials(OAuthGrantClientCredentialsContext context)
        {
            var identity = new ClaimsIdentity(new GenericIdentity(context.ClientId, OAuthDefaults.AuthenticationType), 
                context.Scope.Select(x => new Claim("urn:oauth:scope", x)));
            context.Validated(identity);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Create a one-time use authentication code.
        /// </summary>
        /// <remarks>
        /// Called when grant type is auth code and we've authenticated.
        /// </remarks>
        private void CreateAuthenticationCode(AuthenticationTokenCreateContext context)
        {
            context.SetToken(Guid.NewGuid().ToString("n") + Guid.NewGuid().ToString("n"));
            // Associate our ticket with the newly created authentication code.
            // We use this code once and the ticket is converted to an access token
            // when the client hands back this authentication code.
            _authenticationCodes[context.Token] = context.SerializeTicket();
        }

        /// <summary>
        /// Received an authentication code from client.
        /// </summary>
        /// <remarks>
        /// Called when grant type is auth code.
        /// Lookup the ticket associated with the authentication code.
        /// Ticket contains claims from authentication.
        /// Katana will convert ticket into an access token.
        /// </remarks>
        private void ReceiveAuthenticationCode(AuthenticationTokenReceiveContext context)
        {
            string value;
            // Remove the authentication code since it's only good once.
            // Deserialize the ticket so Katana can generate an access token.
            if (_authenticationCodes.TryRemove(context.Token, out value))
                context.DeserializeTicket(value);
        }

        /// <summary>
        /// Create a new access token from a ticket.
        /// </summary>
        /// <remarks>
        /// This method is called when creating a new access token from a ticket.
        /// </remarks>
        private void CreateRefreshToken(AuthenticationTokenCreateContext context)
        {
            context.SetToken(context.SerializeTicket());
        }

        /// <summary>
        /// We received a refresh request from the client with a given access token.
        /// </summary>
        /// <remarks>
        /// Method deserializes the tiket from the existing access token,
        /// then Katana will create a new access token that it sends back to the
        /// client.
        /// </remarks>
        private void ReceiveRefreshToken(AuthenticationTokenReceiveContext context)
        {
            context.DeserializeTicket(context.Token);
        }
    }
}