using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;

namespace OAuth.AuthorizationServer.Middleware
{
    /// <summary>
    /// Handler inserted into OWIN pipeline to do the work.
    /// </summary>
    public class DummyAuthenticationHandler : AuthenticationHandler<DummyAuthenticationOptions>
    {
        #region Methods

        /// <summary>
        /// Look for incoming tokens and convert them to an authentication ticket.
        /// </summary>
        protected override Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            // ASP.Net Identity requires the NameIdentitifer field to be set or it won't  
            // accept the external login (AuthenticationManagerExtensions.GetExternalLoginInfo)
            var identity = new ClaimsIdentity(Options.SignInAsAuthenticationType);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, Options.UserName, null, Options.AuthenticationType));
            identity.AddClaim(new Claim(ClaimTypes.Name, Options.UserName));
            var properties = Options.StateDataFormat.Unprotect(Request.Query["state"]);
            return Task.FromResult(new AuthenticationTicket(identity, properties));
        }

        /// <summary>
        /// If the Application has issued a 401 then issue the challenge to the client.
        /// </summary>
        protected override Task ApplyResponseChallengeAsync()
        {
            if (Response.StatusCode != 401) return Task.FromResult<object>(null);
            var challenge = Helper.LookupChallenge(Options.AuthenticationType, Options.AuthenticationMode);
            // Only react to 401 if there is an authentication challenge for the authentication 
            // type of this handler.
            if (challenge == null) return Task.FromResult<object>(null);
            var state = challenge.Properties;
            if (string.IsNullOrEmpty(state.RedirectUri))
                state.RedirectUri = Request.Uri.ToString();
            var stateString = Options.StateDataFormat.Protect(state);
            Response.Redirect(WebUtilities.AddQueryString(Options.CallbackPath.Value, "state", stateString));
            return Task.FromResult<object>(null);
            
        }

        /// <summary>
        /// Process authentication callback, if it is one.
        /// </summary>
        public override async Task<bool> InvokeAsync()
        {
            // This is always invoked on each request. For passive middleware, only do anything if this is
            // for our callback path when the user is redirected back from the authentication provider.
            if (!Options.CallbackPath.HasValue || Options.CallbackPath != Request.Path) return false;
            var ticket = await AuthenticateAsync();
            // No ticket, let the rest of the pipeline run.
            if (ticket == null) return false;
            Context.Authentication.SignIn(ticket.Properties, ticket.Identity);
            Response.Redirect(ticket.Properties.RedirectUri);
            // Prevent further processing by the owin pipeline.
            return true;
        }

        #endregion Methods
    }
}
