
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace EXODemo.Console
{
    static class EXOHelper
    {
        public class MailBoxItem
        {
            public string Subject { get; private set; }
            public DateTime Received { get; private set; }
            public string From { get; private set; }
            public string Body { get; set; }
            public MailBoxItem(string subject, DateTime received, string from, string body)
            {
                Subject = subject;
                Received = received;
                From = from;
                Body = body;
            }
        }

        // Use the following scope if using Client Credentials (Assertion):
        // "https://outlook.office365.com/.default"
        public static async Task<IEnumerable<MailBoxItem>> QueryMailboxWithEWSAsync(string accessToken, string mailboxName, string impersonateUsername = null)
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                var results = new List<MailBoxItem>();
                var client = new ExchangeService()
                {
                    Url = new Uri("https://outlook.office365.com/EWS/Exchange.asmx"),
                    Credentials = new OAuthCredentials(accessToken),
                    EnableScpLookup = false
                };
                // If operating in an app-only mode we need to impersonate a user.
                if (!string.IsNullOrEmpty(impersonateUsername))
                    client.ImpersonatedUserId = new ImpersonatedUserId(ConnectingIdType.SmtpAddress, impersonateUsername);
                // Query the inbox of the specified user.
                client.HttpHeaders.Add("X-AnchorMailbox", mailboxName);
                var mailbox = new Mailbox(mailboxName);
                var folderId = new FolderId(WellKnownFolderName.Inbox, mailbox);
                var inbox = Microsoft.Exchange.WebServices.Data.Folder.Bind(client, folderId);
                var itemView = new ItemView(10);
                FindItemsResults<Item> filteredItems;
                //do
                //{
                    filteredItems = client.FindItems(inbox.Id, itemView);
                    foreach (var i in filteredItems.Items)
                    {
                        i.Load();
                        results.Add(new MailBoxItem(i.Subject, i.DateTimeReceived, i.InReplyTo, i.TextBody));
                    }
                    itemView.Offset += filteredItems.Items.Count;
                //}
                //while (filteredItems.MoreAvailable);
                return results;
            });
        }

        // Use the following scope if using Client Credentials (Assertion):
        // "https://graph.microsoft.com/.default"
        public static async Task<IEnumerable<MailBoxItem>> QueryMailboxWithGraphAsync(string accessToken, string mailboxName)
        {
            return await System.Threading.Tasks.Task.Run(async () =>
            {
                var results = new List<MailBoxItem>();
                var client = new GraphServiceClient(new DelegateAuthenticationProvider(requestMessage => {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    return System.Threading.Tasks.Task.FromResult(0);
                }));
                var messages = await client.Users[mailboxName].Messages.Request().GetAsync();
                while (null != messages.NextPageRequest)
                {
                    foreach (var message in messages.CurrentPage)
                    {
                        results.Add(new MailBoxItem(
                            message.Subject,
                            message.ReceivedDateTime.GetValueOrDefault().DateTime,
                            message.From.EmailAddress.Address,
                            message.Body.Content));
                    }
                    messages = await messages.NextPageRequest.GetAsync();
                }
                return results;
            });
        }
    }
}