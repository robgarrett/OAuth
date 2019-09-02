using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace CoreMVC
{
    public static class OAuth2
    {
        public class OAuth2Provider
        {
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
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

        /// <summary>
        /// Get the Uri for the given provider to obtain the access code.
        /// </summary>
        /// <param name="provider">Provider details.</param>
        /// <param name="redirectUri">Redirect Uri that the provider calls.</param>
        /// <param name="locale">Locale</param>
        /// <returns>Uri to call for the provider.</returns>
        public static Uri CreateRedirect(OAuth2Provider provider, Uri redirectUri, string locale = "en")
        {
            if (null == provider) throw new ArgumentNullException(nameof(provider));
            if (null == redirectUri) throw new ArgumentNullException(nameof(redirectUri));
            if (null == provider.AuthUri) throw new ArgumentException("Authorization Uri is blank");
            if (string.IsNullOrEmpty(provider.ClientId)) throw new ArgumentException("Client Id is blank");
            var parameters = new Dictionary<string, string>
            {
                { "client_id", provider.ClientId},
                // { "display", "page" },
                // { "locale", locale },
                { "redirectUri", redirectUri.ToString() },
                { "response_type", "code" }
            };
            if (provider.Offline)
                parameters.Add("access_type", "offline");
            if (!string.IsNullOrWhiteSpace(provider.Scope))
                parameters.Add("scope", provider.Scope);
            if (!string.IsNullOrWhiteSpace(provider.State))
                parameters.Add("state", provider.State);
            var qs = BuildQueryString(parameters);
            return new Uri($"{provider.AuthUri}?{qs}");
        }

        /// <summary>
        /// Get the OAuth access token with a given OAuth code.
        /// </summary>
        /// <param name="provider">Provider.</param>
        /// <param name="redirectUri">Redirect Uri that provider calls.</param>
        /// <param name="code">OAuth code.</param>
        /// <returns>Access token details.</returns>
        public static OAuth2AuthenticationResponse AuthenticateByCode(OAuth2Provider provider, Uri redirectUri, string code)
        {
            if (null == provider) throw new ArgumentNullException(nameof(provider));
            if (null == redirectUri) throw new ArgumentNullException(nameof(redirectUri));
            if (string.IsNullOrEmpty(code)) throw new ArgumentNullException(nameof(code));
            if (string.IsNullOrEmpty(provider.ClientId)) throw new ArgumentException("Client Id is blank");
            if (string.IsNullOrWhiteSpace(provider.ClientSecret)) throw new ArgumentException("Client secret is blank");
            if (null == provider.AccessTokenUri) throw new ArgumentException("Access Token Uri is blank");
            var parameters = new Dictionary<string, string>
            {
                {"client_id", provider.ClientId},
                { "client_secret", provider.ClientSecret },
                { "code", code },
                { "redirectUri", redirectUri.ToString() },
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

        /// <summary>
        /// Authenticate by getting a new access token given a refresh token.
        /// </summary>
        /// <param name="provider">Provider.</param>
        /// <param name="refreshToken">Refresh token.</param>
        /// <returns>Access token details.</returns>
        public static OAuth2AuthenticationResponse AuthenticateByToken(OAuth2Provider provider, string refreshToken)
        {
            if (null == provider) throw new ArgumentNullException(nameof(provider));
            if (string.IsNullOrEmpty(refreshToken)) throw new ArgumentNullException(nameof(refreshToken));
            if (string.IsNullOrEmpty(provider.ClientId)) throw new ArgumentException("Client Id is blank");
            if (string.IsNullOrWhiteSpace(provider.ClientSecret)) throw new ArgumentException("Client secret is blank");
            if (null == provider.AccessTokenUri) throw new ArgumentException("Access Token Uri is blank");
            var parameters = new Dictionary<string, string>
            {
                {"client_id", provider.ClientId},
                { "client_secret", provider.ClientSecret },
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
            return parameters.Aggregate("", (c, p) => $"{c}&{p.Key}={HttpUtility.UrlEncode(p.Value)}").Substring(1);
        }
    }
}