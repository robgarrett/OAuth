using Microsoft.AspNet.Identity.EntityFramework;

namespace OAuth.AuthorizationServer.Models
{
    /// <summary>
    /// ASP.NET Identity EF DB Context for OAUTH Authorization Server.
    /// </summary>
    /// <remarks>
    /// Initializes a UserStore and UserManager baecause we inherit from
    /// an IdentityDbContext. Add additional properties to store in our context.
    /// </remarks>
    public class OAuthDbContext : IdentityDbContext<OAuthUser>
    {
        #region Construction

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// DefaultConnection refers to the connection string for the 
        /// default database in the App_Data folder.
        /// </remarks>
        public OAuthDbContext() : base("DefaultConnection")
        {
        }

        #endregion Construction

        #region Methods

        internal static OAuthDbContext Create()
        {
            return new OAuthDbContext();
        }

        #endregion Methods
    }
}