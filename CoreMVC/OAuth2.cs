using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace CoreMVC
{
    public static class OAuth2
    {
        public class OAuth2Provider
        {
            public Guid ClientId { get; set; }
            public string CertSubjectName { get; set; }
            public string Authority { get; set; }
            public Uri AuthUri { get; set; }
            public Uri AccessTokenUri { get; set; }
            public Uri UserInfoUri { get; set; }
            public string Scope { get; set; }
            public string State { get; set; }
            public bool Offline { get; set; }

            public OAuth2Provider()
            {
                Offline = false;
            }
        }

        public class OAuth2AuthenticationResponse
        {
            public string AccessToken { get; internal set; }
            public string RefreshToken { get; internal set; }
            public string IDToken { get; internal set; }
            public DateTime Expires { get; internal set; }
            public string State { get; internal set; }

            public static OAuth2AuthenticationResponse Parse(string data)
            {
                if (string.IsNullOrWhiteSpace(data)) throw new ArgumentNullException(nameof(data));
                var response = new OAuth2AuthenticationResponse();
                if (data.StartsWith("{"))
                {
                    var dict = (Dictionary<string, string>)JsonConvert.DeserializeObject(data, typeof(Dictionary<string, string>));
                    if (dict.ContainsKey("access_token")) response.AccessToken = dict["access_token"];
                    if (dict.ContainsKey("id_token")) response.IDToken = dict["id_token"];
                    if (dict.ContainsKey("refresh_token")) response.RefreshToken = dict["refresh_token"];
                    if (dict.ContainsKey("state")) response.State = dict["state"];
                    var seconds = 0;
                    if (dict.ContainsKey("expires")) int.TryParse(dict["expires"], out seconds);
                    if (dict.ContainsKey("expires_in")) int.TryParse(dict["expires_in"], out seconds);
                    if (seconds > 0) response.Expires = DateTime.Now.AddSeconds(seconds);
                }
                else if (data.Contains("&"))
                {
                    var dict = data.Split('&');
                    foreach (var entry in dict)
                    {
                        var index = entry.IndexOf("=", StringComparison.Ordinal);
                        if (index < 0) continue;
                        var key = entry.Substring(0, index);
                        var value = entry.Substring(index + 1);
                        switch (key.ToLower())
                        {
                            case "access_token":
                                response.AccessToken = value;
                                break;
                            case "refresh_token":
                                response.RefreshToken = value;
                                break;
                            case "state":
                                response.State = value;
                                break;
                            case "expires":
                            case "expires_in":
                                if (int.TryParse(value, out var seconds))
                                    response.Expires = DateTime.Now.AddSeconds(seconds);
                                break;
                        }
                    }
                }

                return response;
            }
        }

        public static Uri CreateRedirect(OAuth2Provider provider, Uri redirectUri)
        {
            string qs;
            var url = CreateRedirect(provider, redirectUri, out qs);
            return new Uri($"{provider.AuthUri}?{qs}");
        }

        public static Uri CreateRedirect(OAuth2Provider provider, Uri redirectUri, out string postPayload)
        {
            if (null == provider) throw new ArgumentNullException(nameof(provider));
            if (null == redirectUri) throw new ArgumentNullException(nameof(redirectUri));
            if (null == provider.AuthUri) throw new ArgumentException("Authorization Uri is blank");
            if (null == provider.ClientId) throw new ArgumentException("Client Id is blank");
            var parameters = new Dictionary<string, string>
            {
                { "client_id", provider.ClientId.ToString()},
                { "redirect_uri", redirectUri.ToString() },
                { "response_mode", "query" },
                { "response_type", "code" }
            };
            if (provider.Offline)
                parameters.Add("access_type", "offline");
            if (!string.IsNullOrWhiteSpace(provider.Scope))
                parameters.Add("scope", provider.Scope);
            if (!string.IsNullOrWhiteSpace(provider.State))
                parameters.Add("state", provider.State);
            postPayload = BuildQueryString(parameters);
            return provider.AuthUri;
        }

        public static OAuth2AuthenticationResponse AuthenticateByCode(OAuth2Provider provider, Uri redirectUri, string code)
        {
            if (null == provider) throw new ArgumentNullException(nameof(provider));
            if (null == redirectUri) throw new ArgumentNullException(nameof(redirectUri));
            if (string.IsNullOrEmpty(code)) throw new ArgumentNullException(nameof(code));
            if (null == provider.ClientId) throw new ArgumentException("Client Id is blank");
            if (string.IsNullOrWhiteSpace(provider.CertSubjectName)) throw new ArgumentException("Cert Subject name is blank");
            if (null == provider.AccessTokenUri) throw new ArgumentException("Access Token Uri is blank");
            if (string.IsNullOrEmpty(provider.Authority)) throw new ArgumentException("Authority is blank");
            // Get the cert to create our JWT.
            var cert = ReadCertFromStore(provider.CertSubjectName);
            if (null == cert) throw new CryptographicException($"Cannot find cert with {provider.CertSubjectName} in the local machine personal store.");
            // Create the JWT to send.
            var jwt = CreateClientAuthJwt(provider, cert);
            // Post.
            var parameters = new Dictionary<string, string>
            {
                { "client_id", provider.ClientId.ToString()},
                { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
                { "client_assertion", jwt },
                { "code", code },
                { "redirect_uri", redirectUri.ToString() },
                { "grant_type", "authorization_code" }
            };
            if (!string.IsNullOrWhiteSpace(provider.Scope))
                parameters.Add("scope", provider.Scope);
            if (!string.IsNullOrWhiteSpace(provider.State))
                parameters.Add("state", provider.State);
            // POST to server.
            var reply = Request(provider.AccessTokenUri, payload: BuildQueryString(parameters));
            return OAuth2AuthenticationResponse.Parse(reply);
        }

        public static OAuth2AuthenticationResponse AuthenticateByToken(OAuth2Provider provider, string refreshToken)
        {
            if (null == provider) throw new ArgumentNullException(nameof(provider));
            if (string.IsNullOrEmpty(refreshToken)) throw new ArgumentNullException(nameof(refreshToken));
            if (null == provider.ClientId) throw new ArgumentException("Client Id is blank");
            if (string.IsNullOrWhiteSpace(provider.CertSubjectName)) throw new ArgumentException("Cert Subject name is blank");
            if (null == provider.AccessTokenUri) throw new ArgumentException("Access Token Uri is blank");
            if (string.IsNullOrEmpty(provider.Authority)) throw new ArgumentException("Authority is blank");
            // Get the cert to create our JWT.
            var cert = ReadCertFromStore(provider.CertSubjectName);
            if (null == cert) throw new CryptographicException($"Cannot find cert with {provider.CertSubjectName} in the local machine personal store.");
            // Create the JWT to send.
            var jwt = CreateClientAuthJwt(provider, cert);
            var parameters = new Dictionary<string, string>
            {
                { "client_id", provider.ClientId.ToString()},
                { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
                { "client_assertion", jwt },
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" }
            };
            if (!string.IsNullOrWhiteSpace(provider.Scope))
                parameters.Add("scope", provider.Scope);
            if (!string.IsNullOrWhiteSpace(provider.State))
                parameters.Add("state", provider.State);
            // POST to server.
            var reply = Request(provider.AccessTokenUri, payload: BuildQueryString(parameters));
            return OAuth2AuthenticationResponse.Parse(reply);
        }

        public static bool ValidateTokens(OAuth2AuthenticationResponse response)
        {
            if (null == response) throw new ArgumentNullException(nameof(response));
            try
            {
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetUserDetails(OAuth2Provider provider, string accessToken)
        {
            if (null == provider) throw new ArgumentNullException(nameof(provider));
            if (string.IsNullOrEmpty(accessToken)) throw new ArgumentNullException(nameof(accessToken));
            if (null == provider.UserInfoUri) return string.Empty;
            var parameters = new Dictionary<string, string> {
                {"access_token", accessToken}
            };
            // GET from the server.
            return Request(provider.UserInfoUri, "GET", BuildQueryString(parameters));
        }

        private static string Request(Uri uri, string method = "POST", string payload = null)
        {
            if (null == uri) throw new ArgumentNullException(nameof(uri));
            var requestUri = uri;
            method = method.ToUpper();
            if (method == "GET" && !string.IsNullOrWhiteSpace(payload))
                requestUri = new Uri($"{uri}?{payload}");
            if (!(WebRequest.Create(requestUri) is HttpWebRequest request))
                throw new WebException("Could not create web request");
            request.Method = method;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Expect = null;
            if (method == "POST" && !string.IsNullOrWhiteSpace(payload))
            {
                var buffer = Encoding.UTF8.GetBytes(payload);
                request.ContentLength = buffer.Length;
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(buffer, 0, buffer.Length);
                    stream.Flush();
                    stream.Close();
                }
            }
            else
            {
                request.ContentLength = 0;
            }
            // Get response from the provider.
            using (var response = request.GetResponse())
            {
                var stream = response.GetResponseStream();
                if (null == stream) throw new WebException("Failed to get response stream");
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        private static string BuildQueryString(IDictionary<string, string> parameters)
        {
            if (null == parameters) throw new ArgumentNullException(nameof(parameters));
            return parameters.Aggregate("", (c, p) => $"{c}&{p.Key}={HttpUtility.UrlEncode(p.Value).Replace("+", "%20")}").Substring(1);
        }

        private static string CreateClientAuthJwt(OAuth2Provider provider, X509Certificate2 cert)
        {
            var tokenHandler = new JwtSecurityTokenHandler { TokenLifetimeInMinutes = 5 };
            var securityToken = tokenHandler.CreateJwtSecurityToken(
                // iss must be the client_id of our application
                issuer: provider.ClientId.ToString(),
                // aud must be the identity provider (token endpoint)
                audience: provider.Authority,
                // sub must be the client_id of our application
                subject: new ClaimsIdentity(new List<Claim> { new Claim("sub", provider.ClientId.ToString()) }),
                // sign with the private key (using RS256 for IdentityServer)
                signingCredentials: new SigningCredentials(new X509SecurityKey(cert), SecurityAlgorithms.RsaSha256)
            );
            return tokenHandler.WriteToken(securityToken);
        }

        private static X509Certificate2 ReadCertFromStore(string certName)
        {
            X509Certificate2 cert = null;
            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates;
                // Look for unexpired certs.
                var currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                // Match the subject name.
                var signingCert = currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certName, false);
                cert = signingCert.OfType<X509Certificate2>().OrderByDescending(c => c.NotBefore).FirstOrDefault();
                store.Close();
                return cert;
            }
        }
    }
}