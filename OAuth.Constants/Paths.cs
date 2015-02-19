
namespace OAuth.Constants
{
    public static class Paths
    {
        /// <summary> 
        /// AuthorizationServer project should run on this URL 
        /// </summary> 
        public const string AuthorizationServerBaseAddress = "http://localhost:1187";

        /// <summary> 
        /// ResourceServer project should run on this URL 
        /// </summary> 
        public const string ResourceServerBaseAddress = "http://localhost:1230";

        /// <summary> 
        /// ImplicitGrant project should be running on this specific port '38515' 
        /// </summary> 
        public const string ImplicitGrantCallBackPath = "http://localhost:38515/Home/SignIn";

        /// <summary> 
        /// AuthorizationCodeGrant project should be running on this URL. 
        /// </summary> 
        public const string AuthorizeCodeCallBackPath = "http://localhost:1391/";

        public const string AuthorizePath = "/OAuth/Auth";
        public const string TokenPath = "/OAuth/Token";
        //public const string LoginPath = "/Account/Login";
        public const string LoginPath = "/signin-dummy";
        public const string LogoutPath = "/Account/Logout";
        public const string MePath = "/api/Me"; 
    }
}
