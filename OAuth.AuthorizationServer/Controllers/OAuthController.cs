using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace OAuth.AuthorizationServer.Controllers
{
    public class OAuthController : Controller
    {
        public ActionResult Auth()
        {
            // We're expecting this call from a web client, so check the 
            // status code.
            if (Response.StatusCode != 200) return View("AuthError");
            // See if we can get the credential ticket from our access token
            // assuming we have one.
            var authentication = HttpContext.GetOwinContext().Authentication;
            var ticket = authentication.AuthenticateAsync("Application").Result;
            var identity = ticket != null ? ticket.Identity : null;
            if (identity == null)
            {
                // No ticket, so challenge user to authenticate with whatever 
                // authentication type specified.
                authentication.Challenge("Application");
                return new HttpUnauthorizedResult();
            }

            // We have a ticket, get the scope, so user knows.
            var scopes = (Request.QueryString.Get("scope") ?? "").Split(' ');
            // If not a post back show user the dialog that asks them to grant access.
            if (Request.HttpMethod != "POST") return View();
            if (!string.IsNullOrEmpty(Request.Form.Get("submit.Grant")))
            {
                // User has clicked the grant access button.
                // Create a new identity object and sign us in.
                identity = new ClaimsIdentity(identity.Claims, "Bearer", identity.NameClaimType, identity.RoleClaimType);
                foreach (var scope in scopes)
                    identity.AddClaim(new Claim("urn:oauth:scope", scope));
                authentication.SignIn(identity);
            }
            // If user requested sign in with a different user, then force sign out and challenge
            // user for authentication. Otherwise, we're done with this call.
            if (string.IsNullOrEmpty(Request.Form.Get("submit.Login"))) return View();
            authentication.SignOut("Application");
            authentication.Challenge("Application");
            return new HttpUnauthorizedResult();
        }
    }
}