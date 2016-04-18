using System.Web.Mvc;

namespace OAuth.ResourceServer.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// Default controller action.
        /// </summary>
        /// <returns>Message.</returns>
        public string Index()
        {
            return "Resource Server - just a default controller";
        }
    }
}