using System;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CoreMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppSettings _settings;

        public HomeController(IConfiguration config)
        {
            config.GetSection("AzureAD").Bind(_settings = new AppSettings());
            var tenantId = _settings.TenantId.ToString();
            _settings.Authority = _settings.Authority.Replace(@"{TenantId}", tenantId, true, CultureInfo.CurrentCulture);
            _settings.AuthUrl = _settings.AuthUrl.Replace(@"{TenantId}", tenantId, true, CultureInfo.CurrentCulture);
            _settings.TokenUrl = _settings.TokenUrl.Replace(@"{TenantId}", tenantId, true, CultureInfo.CurrentCulture);
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
                    ClientId = _settings.ClientId,
                    Authority = _settings.Authority,
                    CertSubjectName = _settings.CertSubjectName,
                    AccessTokenUri = new Uri(_settings.TokenUrl),
                    State = "AzureAd"
                };
                var response = OAuth2.AuthenticateByCode(provider, _settings.RedirectUri, code);
                if (null == response) throw new Exception("Null response from OAUTH provider.");
                if (OAuth2.ValidateTokens(response))
                    ViewBag.Response = JsonConvert.SerializeObject(response, Formatting.None);
                else
                    ViewBag.Response = "{ \"error\": \"Response failed validation\" }";
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
                    ClientId = _settings.ClientId,
                    Authority = _settings.Authority,
                    CertSubjectName = _settings.CertSubjectName,
                    AuthUri = new Uri(_settings.AuthUrl),
                    Scope = CreateScopesString(),
                    State = "AzureAd"
                };
                var url = OAuth2.CreateRedirect(provider, _settings.RedirectUri);
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
