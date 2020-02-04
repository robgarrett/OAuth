using System;
using System.Collections.Generic;

namespace CoreMVC
{
    public class AppSettings
    {
        public string AuthUrl { get; set; }
        public string TokenUrl { get; set; }
        public string Authority { get; set; }
        public Guid TenantId { get; set; }
        public Guid ClientId { get; set; }
        public string CertSubjectName { get; set; }
        public Uri RedirectUri { get; set; }
        public IEnumerable<string> Audiences { get; set; }
    }
}