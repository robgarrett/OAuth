
using System;
using System.Text.RegularExpressions;

namespace spoauth
{
    public static class UrlHelper
    {
        public static string removeTrailingSlash(string url)
        {
            return Regex.Replace(url, "(/$)|(\\$)", "");
        }

        public static string removeLeadingSlash(string url)
        {
            return Regex.Replace(url, "(^/)|(^\\)", "");
        }

        public static string trimSlashes(string url)
        {
            return Regex.Replace(url, "(^/)|(^\\)|(/$)|(\\$)", "");
        }

        public static HostingEnvironment resolveHostingEnvironment(string url)
        {
            var host = new Uri(url).Host;
            if (-1 != host.IndexOf(".sharepoint.com"))
                return HostingEnvironment.Production;
            else if (-1 != host.IndexOf(".sharepoint.cn"))
                return HostingEnvironment.China;
            else if (-1 != host.IndexOf(".sharepoint.de"))
                return HostingEnvironment.German;
            else if (-1 != host.IndexOf(".sharepoint-mil.us"))
                return HostingEnvironment.USDefense;
            else if (-1 != host.IndexOf(".sharepoint.us"))
                return HostingEnvironment.USGovernment;
            else
                return HostingEnvironment.Production;
        }
    }
}