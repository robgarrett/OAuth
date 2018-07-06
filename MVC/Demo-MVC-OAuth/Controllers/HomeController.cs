using System;
using System.Configuration;
using System.Web.Mvc;

namespace Demo_MVC_OAuth.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var code = Request.QueryString["code"];
            var state = Request.QueryString["state"];
            if (string.IsNullOrEmpty(code)) return View();
            if (string.IsNullOrEmpty(state)) return View("Error");
            // Exchange the code for an access token.
            state = state.ToLower();
            if (state == "github")
            {
                var provider = new OAuth2.OAuth2Provider()
                {
                    ClientId = ConfigurationManager.AppSettings["ClientId"],
                    ClientSecret = ConfigurationManager.AppSettings["ClientSecret"],
                    AccessTokenUri = new Uri("https://github.com/login/oauth/access_token"),
                    UserInfoUri = new Uri("https://api.github.com/user"),
                    State = "GitHub"
                };
                var response = OAuth2.AuthenticateByCode(provider, Request.UrlHome(), code);
                if (null == response) return View("Error");
                // Use the API here...
            }
            return View();
        }

        [HttpPost]
        public ActionResult ProcessForm(string submit)
        {
            if (string.IsNullOrEmpty(submit)) return View("Index");
            submit = submit.ToLower();
            if (submit == "github")
            {
                var provider = new OAuth2.OAuth2Provider()
                {
                    ClientId = ConfigurationManager.AppSettings["ClientId"],
                    ClientSecret = ConfigurationManager.AppSettings["ClientSecret"],
                    AuthUri = new Uri("https://github.com/login/oauth/authorize"),
                    State = "GitHub"
                };
                var url = OAuth2.CreateRedirect(provider, Request.UrlHome());
                return Redirect(url.ToString());
            }
            return View("Index");
        }
    }
}