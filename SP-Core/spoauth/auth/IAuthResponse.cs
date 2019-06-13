using System.Collections.Generic;

namespace spoauth
{
    public interface IAuthResponse
    {
        IDictionary<string, string> headers { get; }
        IDictionary<string, string> options { get; }
    }
}
