using System;
using Microsoft.AspNetCore.Http;

namespace CoreMVC
{
    internal static class Helper
    {
        public static Uri UrlOriginal(this HttpRequest request)
        {
            return new Uri($"{request.Scheme}://{request.Host}{request.Path}");
        }

        public static Uri UrlHome(this HttpRequest request)
        {
            return new Uri($"{request.Scheme}://{request.Host}/Home/Index");
        }
    }
}