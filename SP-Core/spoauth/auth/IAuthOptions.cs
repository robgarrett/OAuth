using System;

namespace spoauth
{
    public interface IBasicOAuthOption
    {
        Guid clientId { get; set; }
    }

    public interface IOnlineAddinCredentials : IBasicOAuthOption
    {
        string clientSecret { get; set; }
        string realm { get; set; }
    }

    public interface IUserCredentials
    {
        string username { get; set; }
        string password { get; set; }
        bool? online { get; set; }

        IUserCredentials Clone(IUserCredentials creds);
    }
}