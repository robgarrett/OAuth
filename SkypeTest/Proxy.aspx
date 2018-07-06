<%@ Page Language="C#" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Net" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Proxy</title>
</head>
<body>
    <form id="MainForm" runat="server">
        <div>
            <%
                var origin = string.IsNullOrEmpty(Request["HTTP_ORIGIN"]) ? Request["HTTP_REFERER"] : Request["HTTP_ORIGIN"];
                if (Request.HttpMethod == "OPTIONS")
                {
                    Response.Headers.Add("Access-Control-Allow-Origin", origin);
                    Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                    Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                    Response.Headers.Add("Access-Control-Allow-Headers", Request["HTTP_ACCESS_CONTROL_REQUEST_HEADERS"]);
                    return;
                }
                // Make an out of bound call to the hub to get the user token.
                var newRequest = WebRequest.CreateHttp(Request.QueryString["url"]);
                var accessToken = Request.QueryString["access_token"];
                newRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
                newRequest.Accept = "application/json";
                newRequest.ContentType = "application/json";
                if (Request.HttpMethod == "POST")
                {
                    // Copy post values.
                    newRequest.Method = "POST";
                    using (var reader = new StreamReader(Request.InputStream))
                    {
                        var body = reader.ReadToEnd();
                        reader.Close();
                        using (var writer = new StreamWriter(newRequest.GetRequestStream()))
                        {
                            writer.Write(body);
                            writer.Flush();
                            writer.Close();
                        }
                    }
                }
                using (var response = newRequest.GetResponse())
                {
                    Response.Headers.Add("Access-Control-Allow-Origin", origin);
                    Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                    Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                    var stream = response.GetResponseStream();
                    if (null != stream)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var body = reader.ReadToEnd();
                            Response.ClearContent();
                            Response.Write(body);
                            Response.End();
                        }
                    }
                }
            %>
        </div>
    </form>
</body>
</html>
