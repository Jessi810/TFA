using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Web;

namespace TFA.Models
{
    public class Image
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }
    }

    public class ImageContext : DbContext
    {
        public ImageContext() : base("Image")
        {
            Database.SetInitializer<ImageContext>(new DropCreateDatabaseIfModelChanges<ImageContext>());
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}