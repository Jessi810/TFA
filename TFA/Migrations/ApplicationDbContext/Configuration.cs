namespace TFA.Migrations.ApplicationDbContext
{
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using Models;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<TFA.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            MigrationsDirectory = @"Migrations\ApplicationDbContext";
        }

        protected override void Seed(TFA.Models.ApplicationDbContext context)
        {
            // Create an admin role if it does not exist.
            if (!context.Roles.Any(r => r.Name == "Admin"))
            {
                var store = new RoleStore<IdentityRole>(context);
                var manager = new RoleManager<IdentityRole>(store);
                var role = new IdentityRole { Name = "Admin" };

                manager.Create(role);
            }

            base.Seed(context);

            // Create a default admin user if it not exist.
            if (!context.Users.Any(u => u.Email == "test.tfa1718@gmail.com"))
            {
                var store = new UserStore<ApplicationUser>(context);
                var manager = new UserManager<ApplicationUser>(store);
                var user = new ApplicationUser
                {
                    Email = "test.tfa1718@gmail.com",
                    EmailConfirmed = true,
                    PhoneNumber = "+639062167820",
                    PhoneNumberConfirmed = true,
                    TwoFactorEnabled = true,
                    LockoutEndDateUtc = null,
                    LockoutEnabled = true,
                    AccessFailedCount = 0,
                    UserName = "test.tfa1718@gmail.com",
                    SerialHash = null,
                    PasswordResetDate = new DateTime(3000, 1, 1, 12, 0, 0)
                };

                manager.Create(user, "Admin@123");
                manager.AddToRole(user.Id, "Admin");
            }

            base.Seed(context);
        }
    }
}
