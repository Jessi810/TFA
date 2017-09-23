namespace TFA.Migrations.ImageContext
{
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.IO;
    using System.Linq;
    using System.Web;

    internal sealed class Config01 : DbMigrationsConfiguration<TFA.Models.ImageContext>
    {
        public Config01()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            MigrationsDirectory = @"Migrations\ImageContext";
        }

        private Models.ImageContext imageDb = new Models.ImageContext();

        protected override void Seed(TFA.Models.ImageContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //

            var images = new List<ImageModels>();
            var files = Directory.GetFiles(System.Web.HttpContext.Current.Server.MapPath("~\\Content\\MyImages"));

            foreach (var file in files)
            {
                var info = new FileInfo(file);

                var image = new ImageModels()
                {
                    Name = Path.GetFileName(file),
                    Path = "localhost:22222/Content/MyImages/" + Path.GetFileName(file)
                };

                imageDb.Images.Add(image);
            }

            imageDb.SaveChanges();
            base.Seed(context);
        }
    }
}
