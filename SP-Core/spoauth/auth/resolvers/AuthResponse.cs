
using System.Collections.Generic;

namespace spoauth
{
    public class AuthResponse : IAuthResponse
    {
        public IDictionary<string, string> headers => new Dictionary<string, string>();
        public IDictionary<string, string> options => new Dictionary<string, string>();
    }
}