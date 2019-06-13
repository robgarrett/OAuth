using System;
using System.Collections.Generic;
using System.Json;
using System.Threading.Tasks;
using System.Xml;

namespace spoauth
{
    public class OnlineUserCredentials : OnlineResolver
    {
        private Cache<string> _cookieCache;
        private IUserCredentials _creds;
        private string OnlineUserRealmEndpoint => $"https://{endpointMappings[hostingEnvironment]}/GetUserRealm.srf";
        private string MSOnlineSts => $"https://{endpointMappings[hostingEnvironment]}/extSTS.srf";
        private string LoginPage => $"https://{endpointMappings[hostingEnvironment]}/login.srf";

        public OnlineUserCredentials(string siteUrl, IUserCredentials creds) : base(siteUrl)
        {
            _cookieCache = new Cache<string>();
            _creds = creds.Clone(creds);
            _creds.username = creds.username
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
            _creds.password = creds.password
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
            initEndpointMappings();
        }

        public async override Task<IAuthResponse> getAuthAsync()
        {
            var host = siteUri.Host;
            var cacheKey = $"{host}@{_creds.username}@{_creds.password}";
            var cachedCookie = _cookieCache.get(cacheKey);
            var tsc = new TaskCompletionSource<IAuthResponse>();
            var authResponse = new AuthResponse();
            if (!string.IsNullOrEmpty(cachedCookie))
            {
                // Use the cache cookie and set result immediately.
                authResponse.headers.Add("Cookie", cachedCookie);
                tsc.SetResult(authResponse);
            }
            // Get the security token, use await so processing of
            // the cookie is done after we have the token.
            var token = await getSecurityTokenAsync();
            var postTokenResp = await PostTokenAsync(token.PageData);
            // Parse the cookie.
            var fedAuth = postTokenResp.Cookies["FedAuth"].Value;
            var rtFa = postTokenResp.Cookies["rtFa"].Value;
            var authCookie = "FedAuth=" + fedAuth + "; rtFa=" + rtFa;
            _cookieCache.set(cacheKey, authCookie);
            authResponse.headers.Add("Cookie", authCookie);
            tsc.SetResult(authResponse);
            return await tsc.Task;
        }

        public override void initEndpointMappings()
        {
            endpointMappings.Add(HostingEnvironment.Production, "login.microsoftonline.com");
            endpointMappings.Add(HostingEnvironment.China, "login.chinacloudapi.cn");
            endpointMappings.Add(HostingEnvironment.German, "login.microsoftonline.de");
            endpointMappings.Add(HostingEnvironment.USDefense, "login-us.microsoftonline.com");
            endpointMappings.Add(HostingEnvironment.USGovernment, "login-us.microsoftonline.com");
        }

        private async Task<WebResponsePayload> getSecurityTokenAsync()
        {
            var tsc = new TaskCompletionSource<WebResponsePayload>();
            try
            {
                // Ask SPO what authentication type to use.
                var postData = $"login={_creds.username}";
                var resp = await PostAsync(this.OnlineUserRealmEndpoint, "application/x-www-form-urlencoded", postData);
                // Parse the response as JSON.
                var respJson = JsonObject.Parse(resp.PageData);
                var authType = (string)respJson["NameSpaceType"];
                if (string.IsNullOrEmpty(authType)) throw new Exception("Unable to determine authentication type.");
                if (0 == string.CompareOrdinal(authType, "Managed"))
                {
                    // Authenticate with cloud credential.
                    var onlineResp = await getSecurityTokenWithOnlineAsync();
                    tsc.SetResult(onlineResp);
                }
                else if (0 == string.CompareOrdinal(authType, "Federated"))
                {
                    // Authenticate with federated credentials.
                    throw new NotImplementedException();
                }
                else
                {
                    throw new Exception($"Unsupported authentication type {authType}.");
                }
            }
            catch (Exception ex)
            {
                tsc.SetException(ex);
            }
            return await tsc.Task;
        }

        private async Task<WebResponsePayload> getSecurityTokenWithOnlineAsync()
        {
            var tsc = new TaskCompletionSource<WebResponsePayload>();
            try
            {
                var spFormsEndpoint = $"{siteUri.GetLeftPart(UriPartial.Authority)}/{Constants.FormsPath}";
                var saml = samlOnlineWsFed.getSaml(_creds, spFormsEndpoint);
                var resp = await PostAsync(MSOnlineSts, "application/soap+xml; charset=utf-8", saml);
                tsc.SetResult(resp);
            }
            catch (Exception ex)
            {
                tsc.SetException(ex);
            }
            return await tsc.Task;
        }

        private async Task<WebResponsePayload> PostTokenAsync(string token)
        {
            var tsc = new TaskCompletionSource<WebResponsePayload>();
            try
            {
                // Parse the token.
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(token);
                var ns = new XmlNamespaceManager(xmlDoc.NameTable);
                ns.AddNamespace("S", "http://www.w3.org/2003/05/soap-envelope");
                ns.AddNamespace("wst", "http://schemas.xmlsoap.org/ws/2005/02/trust");
                ns.AddNamespace("wsu", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
                var securityTokenResponse = xmlDoc.SelectSingleNode("//S:Body", ns).FirstChild;
                if (securityTokenResponse.Name.IndexOf("Fault") >= 0)
                    throw new Exception(securityTokenResponse.InnerText);
                var binaryToken = securityTokenResponse.SelectSingleNode("wst:RequestedSecurityToken", ns).FirstChild.InnerText;
                var expires = DateTime.Parse(securityTokenResponse.SelectSingleNode("wst:Lifetime/wsu:Expires", ns).InnerText);
                var diffSeconds = (expires - DateTime.Now).TotalSeconds;
                // Post the token.
                var spFormsEndpoint = $"{siteUri.GetLeftPart(UriPartial.Authority)}/{Constants.FormsPath}";
                var resp = await PostAsync(spFormsEndpoint, "application/x-www-form-urlencoded", binaryToken);
                tsc.SetResult(resp);
            }
            catch (Exception ex)
            {
                tsc.SetException(ex);
            }
            return await tsc.Task;
        }
    }
}