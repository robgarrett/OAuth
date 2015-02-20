using System;
using Microsoft.Owin.Security;

namespace OAuth.AuthorizationServer.Middleware
{
    /// <summary>
    /// Options for the BasicAuthentication middleware pipeline.
    /// </summary>
    public class BasicAuthenticationOptions : AuthenticationOptions
    {
        #region Properties

        /// <summary>
        /// Delegate to validate basic credentials.
        /// </summary>
        public Func<string, string, bool> ValidateCredentials { get; set; }

        #endregion Properties

        #region Construction

        public BasicAuthenticationOptions() : base("Basic")
        {
            Description.Caption = "Basic";
            AuthenticationMode = AuthenticationMode.Active;
        }

        public BasicAuthenticationOptions(string authenticationType)
            : base(authenticationType)
        {
            Description.Caption = "Basic";
            AuthenticationMode = AuthenticationMode.Active;
        }

        #endregion Construction
    }
}