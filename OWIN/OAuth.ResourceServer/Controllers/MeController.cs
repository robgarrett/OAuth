using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web.Http;

namespace OAuth.ResourceServer.Controllers
{
    public class MeController : ApiController
    {
        [Authorize]
        public IEnumerable<object> Get()
        {
            var identity = User.Identity as ClaimsIdentity;
            if (null == identity) throw new NullReferenceException();
            return identity.Claims.Select(c => new { c.Type, c.Value });
        } 
    }
}
