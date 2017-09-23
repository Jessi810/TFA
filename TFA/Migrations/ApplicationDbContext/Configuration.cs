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
                UserName = "test.tfa1718@gmail.com"
            };

            manager.Create(user, "Admin@123");
            manager.AddToRole(user.Id, "Admin");
        }
    }
}
