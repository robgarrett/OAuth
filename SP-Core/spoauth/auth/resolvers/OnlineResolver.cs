
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace spoauth
{
    public abstract class OnlineResolver : IAuthResolver
    {
        public class WebResponsePayload
        {
            public CookieCollection Cookies { get; private set; }
            public string PageData { get; private set; }

            private WebResponsePayload() { }

            public static WebResponsePayload Create(HttpWebResponse resp)
            {
                var result = new WebResponsePayload();
                result.Cookies = new CookieCollection();
                result.Cookies.Add(resp.Cookies);
                using (var reader = new StreamReader(resp.GetResponseStream()))
                {
                    result.PageData = reader.ReadToEnd();
                    reader.Close();
                }
                return result;
            }
        }

        protected HostingEnvironment hostingEnvironment { get; }
        protected IDictionary<HostingEnvironment, string> endpointMappings { get; }
        protected Uri siteUri { get; }

        protected OnlineResolver(string siteUrl)
        {
            endpointMappings = new Dictionary<HostingEnvironment, string>();
            hostingEnvironment = UrlHelper.resolveHostingEnvironment(siteUrl);
            siteUri = new Uri(siteUrl);
        }

        public abstract Task<IAuthResponse> getAuthAsync();

        public abstract void initEndpointMappings();

        protected Task<WebResponsePayload> PostAsync(string url, string contentType = null, string postData = null)
        {
            var tsc = new TaskCompletionSource<WebResponsePayload>();
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            try
            {
                if (!string.IsNullOrEmpty(contentType))
                    req.ContentType = contentType;
                req.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
                req.AllowAutoRedirect = false;
                req.CookieContainer = new CookieContainer();
                if (!string.IsNullOrEmpty(postData))
                {
                    // Post request data first (synchronously).
                    var bytedata = System.Text.Encoding.UTF8.GetBytes(postData);
                    using (var stream = req.GetRequestStream())
                    {
                        stream.Write(bytedata, 0, bytedata.Length);
                        stream.Close();
                    }
                }
                // Post data asynchronously and get result as a task.
                req.BeginGetResponse(asyncResult =>
                {
                    try
                    {
                        var resp = req.EndGetResponse(asyncResult);
                        tsc.SetResult(WebResponsePayload.Create((HttpWebResponse)resp));
                    }
                    catch (WebException ex)
                    {
                        var resp = ex.Response as HttpWebResponse;
                        if (resp.StatusCode == HttpStatusCode.Found)
                        {
                            // We likely got the cookies and status code
                            // refects redirect to the home page.
                            tsc.SetResult(WebResponsePayload.Create((HttpWebResponse)resp));
                        }
                        else
                        {
                            throw;
                        }
                    }
                }, null);
            }
            catch (Exception ex)
            {
                // Ack, something went wrong.
                tsc.SetException(ex);
            }
            return tsc.Task;
        }
    }
}