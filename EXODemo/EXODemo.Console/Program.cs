using System.Security;
using System.Configuration;
using s = System;

namespace EXODemo.Console
{
    class Program
    {
        private static void RenderEmails(string accessToken, string mailboxName)
        {
            var t = EXOHelper.QueryMailboxWithGraphAsync(accessToken, mailboxName);
            t.Wait();
            foreach (var mailItem in t.Result)
                s.Console.WriteLine($"{mailItem.From}: {mailItem.Subject}");
        }

        static void Main()
        {
            var loginHint = ConfigurationManager.AppSettings["LoginHint"];
            var mailboxUser = ConfigurationManager.AppSettings["mailboxUser"];
            // Client assertion.
            var tokenCert = OauthHelper.GetAccessTokenWithCertificate().Result;
            s.Console.WriteLine("Token with Certificate:");
            s.Console.WriteLine(tokenCert);
            //RenderEmails(tokenCert, mailboxUser);
            s.Console.WriteLine();
            // Federated credentials (assuming SSA).
            if (!string.IsNullOrEmpty(loginHint) && loginHint.EndsWith("ssa.gov", s.StringComparison.OrdinalIgnoreCase))
            {
                var tokenFed = OauthHelper.GetAccessTokenWithFederatedCredentials().Result;
                s.Console.WriteLine("Token with Federated User:");
                s.Console.WriteLine(tokenFed);
                s.Console.WriteLine();
            }
            // Grant web flow.
            var tokenInt = OauthHelper.GetAccessTokenInteractive().Result;
            s.Console.WriteLine("Token with interactive web flow:");
            s.Console.WriteLine(tokenInt);
            RenderEmails(tokenInt, mailboxUser);
            s.Console.WriteLine();
            // Username/Password grant.
            var username = ConfigurationManager.AppSettings["Username"];
            var tokenCreds = OauthHelper.GetAccessTokenWithUsernamePassword(username, ReadPassword(username)).Result;
            s.Console.WriteLine("Token with Username/Password:");
            s.Console.WriteLine(tokenCreds);
            RenderEmails(tokenCreds, mailboxUser);
            s.Console.WriteLine();
        }

        static SecureString ReadPassword(string username)
        {
            s.Console.WriteLine($"Password or {username}:");
            var pwd = new SecureString();
            while (true)
            {
                var i = s.Console.ReadKey(true);
                if (i.Key == s.ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == s.ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.RemoveAt(pwd.Length - 1);
                        s.Console.Write("\b \b");
                    }
                }
                else if (i.KeyChar != '\u0000')
                // KeyChar == '\u0000' if the key pressed does not correspond to a printable character, e.g. F1, Pause-Break, etc
                {
                    pwd.AppendChar(i.KeyChar);
                    s.Console.Write("*");
                }
            }
            return pwd;
        }
    }
}
