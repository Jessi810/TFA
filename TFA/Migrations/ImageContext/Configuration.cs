namespace TFA.Migrations.ImageContext
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.IO;
    using System.Linq;
    using System.Web;

    internal sealed class Configuration : DbMigrationsConfiguration<TFA.Models.ImageContext>
    {
        private TFA.Models.Image db = new Models.Image();

        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            MigrationsDirectory = @"Migrations\ImageContext";
        }

        protected override void Seed(TFA.Models.ImageContext context)
        {
            var images = new List<TFA.Models.Image>();
            var files = Directory.GetFiles(System.Web.HttpContext.Current.Server.MapPath("~/Images"));

            foreach (var file in files)
            {
                var info = new FileInfo(file);

                var image = new TFA.Models.Image()
                {
                    Name = Path.GetFileName(file),
                    Path = "http://localhost:22222/Images/" + Path.GetFileName(file)
                };
            }

            //db.SaveChanges();
            //base.Seed(context);
        }
    }
}
