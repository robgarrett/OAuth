using System.Threading.Tasks;

namespace spoauth
{
    public interface IAuthResolver
    {
        Task<IAuthResponse> getAuthAsync();
    }
}
