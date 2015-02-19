using System.Data.Entity.Migrations;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using OAuth.AuthorizationServer.Models;

namespace OAuth.AuthorizationServer.Migrations
{

    internal sealed class Configuration : DbMigrationsConfiguration<Models.OAuthDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }

        protected override void Seed(OAuthDbContext context)
        {
            //  This method will be called after migrating to the latest version.
            var manager = new UserManager<OAuthUser>(new UserStore<OAuthUser>(new OAuthDbContext()));
            // Create some test users.
            manager.Create(new OAuthUser { UserName = "rgarrett"}, "Password");
        }
    }
}
