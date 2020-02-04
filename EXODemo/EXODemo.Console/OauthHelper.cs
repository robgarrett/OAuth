using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace EXODemo.Console
{
    static class OauthHelper
    {
        class AuthOptions
        {
            public bool IsInteractive { get; set; }
            public bool IsFederated { get; set; }
            public NetworkCredential Credentials { get; set; }
        }

        public static async Task<string> GetAccessTokenInteractive()
        {
            var authOptions = new AuthOptions { IsInteractive = true };
            return await AuthenticateUser(authOptions);
        }

        public static async Task<string> GetAccessTokenWithFederatedCredentials()
        {
            // Use the current username and password.
            var authOptions = new AuthOptions { IsInteractive = false, IsFederated = true };
            return await AuthenticateUser(authOptions);
        }
       
        public static async Task<string> GetAccessTokenWithUsernamePassword(string username, SecureString password)
        {
            // WARNING: Use of this flow is highly discouraged - it's not supported by CAS policy 
            // and won't work with two-factor authentication.
            var creds = new NetworkCredential(username, password);
            var authOptions = new AuthOptions { IsInteractive = false, IsFederated = false, Credentials = creds };
            return await AuthenticateUser(authOptions);
        }

        public static async Task<string> GetAccessTokenWithCertificate(string staticScope = "https://graph.microsoft.com/.default")
        {
            var subjectName = ConfigurationManager.AppSettings["CertSubjectName"];
            var cert = ReadCertFromStore(subjectName);
            // When using non-interactive scopes, use the static scope from Graph so we
            // can assign them in the portal.
            var scopes = new [] { staticScope };
            var options = new ConfidentialClientApplicationOptions
            {
                TenantId = ConfigurationManager.AppSettings["TenantId"],
                ClientId = ConfigurationManager.AppSettings["ClientId"],
                RedirectUri = ConfigurationManager.AppSettings["RedirectUri"]
            };
            AuthenticationResult result;
            var app = ConfidentialClientApplicationBuilder.CreateWithApplicationOptions(options).WithCertificate(cert).Build();
            result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
            return result.AccessToken;
        }

        public static X509Certificate2 ReadCertFromStore(string certName)
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

        private static IEnumerable<string> GetScopes()
        {
            var scopes = ConfigurationManager.AppSettings["Scopes"];
            foreach (var scope in scopes.Split(new[] { ' ' }))
                yield return scope.Trim();
        }

        private static async Task<string> AuthenticateUser(AuthOptions authOptions = null)
        {
            var scopes = GetScopes();
            var options = new PublicClientApplicationOptions
            {
                TenantId = ConfigurationManager.AppSettings["TenantId"],
                ClientId = ConfigurationManager.AppSettings["ClientId"],
                RedirectUri = ConfigurationManager.AppSettings["RedirectUri"],
                AadAuthorityAudience = AadAuthorityAudience.AzureAdMyOrg
            };
            var app = PublicClientApplicationBuilder.CreateWithApplicationOptions(options).Build();
            var accounts = await app.GetAccountsAsync();
            AuthenticationResult result;
            try
            {
                // Try silent first.
                result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                if (null == authOptions || authOptions.IsInteractive)
                {
                    result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
                }
                else if (authOptions.IsFederated)
                {
                    // Note: We cannot use AcquireTokenByIntegratedWindowsAuth because this assumes ADFS and existence
                    // of MEX document URL, which SSA doesn't have.
                    var loginHint = ConfigurationManager.AppSettings["LoginHint"];
                    result = await app.AcquireTokenInteractive(scopes)
                        .WithLoginHint(loginHint)
                        .WithPrompt(Prompt.NoPrompt).ExecuteAsync();
                }
                else
                {
                    if (null == authOptions.Credentials) throw new ArgumentException("Credentials empty.");         
                    result = await app.AcquireTokenByUsernamePassword(
                        scopes, authOptions.Credentials.UserName, authOptions.Credentials.SecurePassword).ExecuteAsync();
                }
            }
            return result.AccessToken;
        }
    }
}
