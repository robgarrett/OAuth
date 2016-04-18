using DotNetOpenAuth.OAuth2;
using Microsoft.AspNet.Identity;
using System;
using System.Net.Http;
using System.Web.Mvc;

namespace OAuth.Clients.Common
{
    public abstract class AuthController : Controller
    {
        private WebServerClient _webServerClient;

        /// <summary>
        /// The endpoint for the authorization server.
        /// </summary>
        /// <remarks>This is the base URL.</remarks>
        protected abstract string AuthorizationServerEndpoint { get; }

        /// <summary>
        /// The endpoint for the resource server.
        /// </summary>
        /// <remarks>This is the base URL.</remarks>
        protected abstract string ResourceServerEndpoint { get; }

        /// <summary>
        /// Client ID.
        /// </summary>
        protected abstract string ClientID { get; }

        /// <summary>
        /// Client Secret.
        /// </summary>
        protected abstract string ClientSecret { get; }

        /// <summary>
        /// Scopes for the authorization server.
        /// </summary>
        protected abstract string[] Scopes { get; }

        /// <summary>
        /// Parameter name passed in the form variables for the Access Token.
        /// </summary>
        protected virtual string AccessTokenParameterName { get { return "AccessToken"; } }

        /// <summary>
        /// Parameter name passed in the form variables for the Refresh Token.
        /// </summary>
        protected virtual string RefreshTokenParameterName { get { return "RefreshToken"; } }

        /// <summary>
        /// Path to Authorize endpoint.
        /// </summary>
        protected virtual string AuthPath {  get { return "/OAuth/Auth"; } }

        /// <summary>
        /// Path to the Token endpoint.
        /// </summary>
        protected virtual string TokenPath { get { return "/OAuth/Token"; } }

        /// <summary>
        /// Request access token from Authorization server.
        /// </summary>
        /// <param name="checkReturnOnly">Check the return from an auth server only.</param>
        protected virtual void Authorize(bool checkReturnOnly = false)
        {
            InitializeWebServerClient();
            // Check the form parameters for access and refresh tokens.
            var accessToken = ViewBag.AccessToken = Request.Form[AccessTokenParameterName] ?? "";
            ViewBag.RefreshToken = Request.Form[RefreshTokenParameterName] ?? "";
            ViewBag.Action = "";

            if (string.IsNullOrEmpty(accessToken))
            {
                // No access token, so we need to authenticate/authorize.
                // Following line looks to see if we have a response from an authorization server,
                // for a previous access token request.
                var authorizationState = _webServerClient.ProcessUserAuthorization(Request);
                if (authorizationState != null)
                {
                    // We got a response from authorization server, so populate the access and refresh tokens.
                    ViewBag.AccessToken = authorizationState.AccessToken;
                    ViewBag.RefreshToken = authorizationState.RefreshToken;
                    ViewBag.Action = Request.Path;
                    return;
                }
            }
            if (checkReturnOnly) return;
            // No previous request pending.
            // Reach out to the authorization server for a new access token. 
            // Provide scopes for user to know what they're authorizing.
            var userAuthorization = _webServerClient.PrepareRequestUserAuthorization(Scopes);
            userAuthorization.Send(HttpContext);
            Response.End();
        }

        /// <summary>
        /// Request a refresh of the access token.
        /// </summary>
        protected virtual void Refresh()
        {
            InitializeWebServerClient();
            // Call out to the authorization server to get a new
            // access token, using the old one.
            var accessToken = Request.Form[AccessTokenParameterName];
            var refreshToken = Request.Form[RefreshTokenParameterName];
            // Check that we have the stale tokens.
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                Authorize();
            }
            else
            {
                var state = new AuthorizationState { AccessToken = accessToken, RefreshToken = refreshToken };
                if (_webServerClient.RefreshAuthorization(state))
                {
                    // Update the current token states.
                    ViewBag.AccessToken = state.AccessToken;
                    ViewBag.RefreshToken = state.RefreshToken;
                }
            }
        }

        /// <summary>
        /// Setup the HTTP client for authenticated API call to the resource server.
        /// </summary>
        /// <param name="del">Delegate to do the work.</param>
        protected virtual void CallApi(Action<HttpClient> del)
        {
            if (null == del) throw new ArgumentNullException("del");
            InitializeWebServerClient();
            var accessToken = Request.Form[AccessTokenParameterName];
            if (string.IsNullOrEmpty(accessToken))
            {
                Authorize();
            }
            else
            {
                var resourceServerUri = new Uri(ResourceServerEndpoint);
                var client = new HttpClient(_webServerClient.CreateAuthorizingHandler(accessToken));
                del(client);
            }
        }

        private void InitializeWebServerClient()
        {
            if (null == _webServerClient)
            {
                var authServerUri = new Uri(AuthorizationServerEndpoint);
                var authorizationServer = new AuthorizationServerDescription
                {
                    AuthorizationEndpoint = new Uri(authServerUri, AuthPath),
                    TokenEndpoint = new Uri(authServerUri, TokenPath)
                };
                _webServerClient = new WebServerClient(authorizationServer, ClientID, new PasswordHasher().HashPassword(ClientSecret));
            }
        }
    }
}
