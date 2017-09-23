using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Web;

namespace TFA.Models
{
    public class ImageModels
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }
    }

    public class ImageContext : DbContext
    {
        public ImageContext() : base("MyImage")
        {
            Database.SetInitializer<ImageContext>(new DropCreateDatabaseAlways<ImageContext>());
        }

        public DbSet<ImageModels> Images { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}