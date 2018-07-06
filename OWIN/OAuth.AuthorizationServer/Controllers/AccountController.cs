using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using OAuth.AuthorizationServer.Models;

namespace OAuth.AuthorizationServer.Controllers
{
    public class AccountController : Controller
    {
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid) return View(model);
            // Get the Owin Authentication Manager
            var authentication = HttpContext.GetOwinContext().Authentication;
            if (!string.IsNullOrEmpty(Request.Form.Get("submit.Signin")))
            {
                // Use clicked the sign in button.
                // TODO: Add code to check the credentials here, using ASP.NET Identity Management.
                // Authentication successful, so sign in.
                authentication.SignIn(
                    new AuthenticationProperties { IsPersistent = model.RememberMe },
                    new ClaimsIdentity(new[] { new Claim(ClaimsIdentity.DefaultNameClaimType, 
                        model.Email) }, "Application"));
            }

            return View();
        }

        public ActionResult Logout()
        {
            // TODO: Add code to sign out user.
            return View();
        }
    }
}