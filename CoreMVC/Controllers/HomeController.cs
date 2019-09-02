using System;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CoreMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppSettings _settings;

        public HomeController(IConfiguration config)
        {
            config.GetSection("AzureAD").Bind(_settings = new AppSettings());
            _settings.Authority = _settings.Authority.Replace("{tenantId}", _settings.TenantId.ToString());
        }

        public IActionResult Index()
        {
            var code = Request.Query["code"];
            var state = Request.Query["state"];
            if (string.IsNullOrEmpty(code)) return View();
            if (string.IsNullOrEmpty(state)) return View();
            // Exchange code for an access token.
            if (0 == string.Compare(state, "AzureAd", StringComparison.OrdinalIgnoreCase))
            {
                var provider = new OAuth2.OAuth2Provider()
                {
                    ClientId = _settings.ClientId.ToString(),
                    ClientSecret = _settings.ClientSecret,
                    AccessTokenUri = new Uri($"{_settings.Authority}/token"),
                    State = "AzureAd"
                };
                var response = OAuth2.AuthenticateByCode(provider, Request.UrlHome(), code);
                if (null == response) throw new Exception("Null response from OAUTH provider.");
                // Get the access token details.
                if (!string.IsNullOrEmpty(response.IDToken))
                    ViewBag.AccessToken = response.IDToken;
                else
                    ViewBag.AccessToken = response.AccessToken;
            }
            return View();
        }

        [HttpPost]
        public IActionResult ProcessForm(string submit)
        {
            if (string.IsNullOrEmpty(submit)) return View("Index");
            if (0 == string.Compare(submit, "azure", StringComparison.OrdinalIgnoreCase))
            {
                // Get an access code.
                var provider = new OAuth2.OAuth2Provider()
                {
                    ClientId = _settings.ClientId.ToString(),
                    ClientSecret = _settings.ClientSecret,
                    AuthUri = new Uri($"{_settings.Authority}/authorize"),
                    Scope = CreateScopesString(),
                    State = "AzureAd"
                };
                var url = OAuth2.CreateRedirect(provider, Request.UrlHome());
                return Redirect(url.ToString());
            }
            return View("Index");
        }

        private string CreateScopesString()
        {
            var sb = new StringBuilder();
            foreach (var scope in _settings.Audiences)
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(scope);
            }
            return sb.ToString();
        }
    }
}
