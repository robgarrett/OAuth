using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace authFunc
{
    public class EchoFunc
    {
        [FunctionName("Echo")]
        public async Task<IActionResult> Echo([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "echo")] HttpRequest req, ILogger log)
        {
            return await Task<IActionResult>.Run(() =>
            {
                var msg = req.Query["msg"];
                var result = (!string.IsNullOrEmpty(msg)) ? $"Hello {msg}" : "Hello";
                return new OkObjectResult(result);
            });
        }
    }
}
