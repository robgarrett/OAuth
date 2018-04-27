using System;
using System.Web;
using JetBrains.Annotations;

namespace Demo_MVC_OAuth
{
    internal static class Helper
    {
        [PublicAPI]
        public static Uri UrlOriginal(this HttpRequestBase request)
        {
            var hostHeader = request.Headers["host"];
            return request.Url != null ? new Uri($"{request.Url.Scheme}://{hostHeader}{request.RawUrl}") : null;
        }

        [PublicAPI]
        public static Uri UrlHome(this HttpRequestBase request)
        {
            var hostHeader = request.Headers["host"];
            return request.Url != null ? new Uri($"{request.Url.Scheme}://{hostHeader}/Home/Index") : null;
        }
    }
}