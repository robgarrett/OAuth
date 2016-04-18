using System;
using System.Web.Mvc;
using OAuth.Constants;
using OAuth.Clients.Common;

namespace OAuth.Clients.AuthCode.Controllers
{
    public class HomeController : AuthController
    {
        protected override string AuthorizationServerEndpoint
        {
            get { return Paths.AuthorizationServerBaseAddress; }
        }

        protected override string ClientID
        {
            get { return "42ff5dad3c274c97a3a7c3d44b67bb42"; }
        }

        protected override string ClientSecret
        {
            get { return "test1234"; }
        }

        protected override string ResourceServerEndpoint
        {
            get { return Paths.ResourceServerBaseAddress; }
        }

        protected override string[] Scopes
        {
            get { return new[] { "One", "Two" }; }
        }

        public ActionResult Index()
        {
            Authorize(true);    // See if we're waiting on a return.
            if (!string.IsNullOrEmpty(Request.Form.Get("submit.Authorize")))
                Authorize();
            else if (!string.IsNullOrEmpty(Request.Form.Get("submit.Refresh")))
                Refresh();
            else if (!string.IsNullOrEmpty(Request.Form.Get("submit.CallApi")))
                CallApi(client =>
                {
                    var resourceServerUri = new Uri(ResourceServerEndpoint);
                    var body = client.GetStringAsync(new Uri(resourceServerUri, Paths.MePath)).Result;
                    ViewBag.ApiResponse = body;
                });
            return View("Home");
        }
   }
}