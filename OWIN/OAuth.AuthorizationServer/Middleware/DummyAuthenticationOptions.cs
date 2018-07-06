using Microsoft.Owin;
using Microsoft.Owin.Security;

namespace OAuth.AuthorizationServer.Middleware
{
    /// <summary>
    /// Options for the DummyAuthentication middleware pipeline.
    /// </summary>
    public class DummyAuthenticationOptions : AuthenticationOptions
    {
        #region Properties

        public PathString CallbackPath { get; set; }
        public string UserName { get; set; }
        public string SignInAsAuthenticationType { get; set; }
        public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; }

        #endregion Properties

        #region Construction

        public DummyAuthenticationOptions(string userName) : base("Dummy")
        {
            Description.Caption = "Dummy";
            CallbackPath = new PathString("/signin-dummy");
            AuthenticationMode = AuthenticationMode.Passive;
            UserName = userName;
        }

        #endregion Construction
    }
}