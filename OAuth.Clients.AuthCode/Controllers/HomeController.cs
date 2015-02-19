using System;
using System.Net.Http;
using System.Web.Mvc;
using DotNetOpenAuth.OAuth2;
using Microsoft.AspNet.Identity;
using OAuth.Constants;

namespace OAuth.Clients.AuthCode.Controllers
{
    public class HomeController : Controller
    {
        private WebServerClient _webServerClient;

        public ActionResult Index()
        {
            ViewBag.AccessToken = Request.Form["AccessToken"] ?? "";
            ViewBag.RefreshToken = Request.Form["RefreshToken"] ?? "";
            ViewBag.Action = "";
            ViewBag.ApiResponse = "";

            InitializeWebServerClient();
            var accessToken = Request.Form["AccessToken"];
            if (string.IsNullOrEmpty(accessToken))
            {
                // No access token, so we need to authenticate.
                // Following line looks to see if we have a response from an authorization server.
                var authorizationState = _webServerClient.ProcessUserAuthorization(Request);
                if (authorizationState != null)
                {
                    ViewBag.AccessToken = authorizationState.AccessToken;
                    ViewBag.RefreshToken = authorizationState.RefreshToken;
                    ViewBag.Action = Request.Path;
                }
            }

            if (!string.IsNullOrEmpty(Request.Form.Get("submit.Authorize")))
            {
                // We clicked the Authorize button, reach out to the authorization server for a new 
                // access token. Provide scopes for user to know what they're authorizing.
                var userAuthorization = _webServerClient.PrepareRequestUserAuthorization(new[] { "bio", "notes" });
                userAuthorization.Send(HttpContext);
                Response.End();
            }
            else if (!string.IsNullOrEmpty(Request.Form.Get("submit.Refresh")))
            {
                // We clicked the Refresh Token button, call out to the authorization server to get a new 
                // access token, using the old one.
                var state = new AuthorizationState
                {
                    AccessToken = Request.Form["AccessToken"],
                    RefreshToken = Request.Form["RefreshToken"]
                };
                if (!_webServerClient.RefreshAuthorization(state)) return View("Home");
                // Update the current token states.
                ViewBag.AccessToken = state.AccessToken;
                ViewBag.RefreshToken = state.RefreshToken;
            }
            else if (!string.IsNullOrEmpty(Request.Form.Get("submit.CallApi")))
            {
                // Call an API call to our resource server, using our authorized context.
                var resourceServerUri = new Uri(Paths.ResourceServerBaseAddress);
                var client = new HttpClient(_webServerClient.CreateAuthorizingHandler(accessToken));
                var body = client.GetStringAsync(new Uri(resourceServerUri, Paths.MePath)).Result;
                ViewBag.ApiResponse = body;
            }

            return View("Home");
        }

        private void InitializeWebServerClient()
        {
            var authorizationServerUri = new Uri(Paths.AuthorizationServerBaseAddress);
            var authorizationServer = new AuthorizationServerDescription
            {
                AuthorizationEndpoint = new Uri(authorizationServerUri, Paths.AuthorizePath),
                TokenEndpoint = new Uri(authorizationServerUri, Paths.TokenPath)
            };
            _webServerClient = new WebServerClient(authorizationServer, "42ff5dad3c274c97a3a7c3d44b67bb42", 
                new PasswordHasher().HashPassword("test1234"));
        }
    }
}