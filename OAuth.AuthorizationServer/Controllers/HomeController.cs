using System.Web.Mvc;

namespace OAuth.AuthorizationServer.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// Default controller action.
        /// </summary>
        /// <returns>Message.</returns>
        public string Index()
        {
            return "Authorization Server - just a default controller";
        }
    }
}