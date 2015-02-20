using System;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;

namespace OAuth.AuthorizationServer.Middleware
{
    /// <summary>
    /// Handler inserted into OWIN pipeline to do the work.
    /// </summary>
    public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationOptions>
    {
        #region Methods

        /// <summary>
        /// Look for incoming tokens and convert them to an authentication ticket.
        /// </summary>
        protected override Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            var emptyTicket = new AuthenticationTicket(null, new AuthenticationProperties());
            // Check the headers for an authorization header of type basic.
            var header = Request.Headers["Authorization"];
            if (String.IsNullOrEmpty(header) || !header.Trim().ToLower().StartsWith("basic"))
                return Task.FromResult(emptyTicket);
            // Decode the header.
            header = header.Trim().Substring(5).Trim(); // Remove the 'Basic'
            header = Encoding.UTF8.GetString(Convert.FromBase64String(header));
            // Look for colon that splits username and password.
            var index = header.IndexOf(":", StringComparison.Ordinal);
            if (-1 == index) return Task.FromResult(emptyTicket);
            var user = header.Substring(0, index);
            var password = header.Substring(index + 1);
            // Validate credentials.
            if (null == Options.ValidateCredentials || !Options.ValidateCredentials(user, password))
                return Task.FromResult(emptyTicket);
            // Validated, create a ticket.
            var identity = new ClaimsIdentity(Options.AuthenticationType);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user, null, Options.AuthenticationType));
            identity.AddClaim(new Claim(ClaimTypes.Name, user));
            var ticket = new AuthenticationTicket(identity, new AuthenticationProperties());
            return Task.FromResult(ticket);
        }

        /// <summary>
        /// If the Application has issued a 401 then issue the challenge to the client.
        /// </summary>
        protected override Task ApplyResponseChallengeAsync()
        {
            if (Response.StatusCode == 401) Response.Headers.Add("WWW-Authenticate", new[] { "Basic" });
            return Task.FromResult<object>(null);
        }

        #endregion Methods
    }
}