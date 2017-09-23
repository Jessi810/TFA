using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TFA.Models
{
    public class SecurityLogModels
    {
        public int Id { get; set; }

        public string Types { get; set; }

        public DateTime Date { get; set; }

        public virtual ApplicationUser User { get; set; }

        public string ApplicationUserId { get; set; }
    }

    public class SecurityLogContext : DbContext
    {
        public SecurityLogContext() : base("MySecurityLog")
        {
            Database.SetInitializer<ImageContext>(new DropCreateDatabaseAlways<ImageContext>());
        }

        public DbSet<SecurityLogModels> Images { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}