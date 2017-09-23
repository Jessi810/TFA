using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Web;

namespace TFA.Models
{
    public class SecLog
    {
        public int Id { get; set; }

        public string Type { get; set; }

        public DateTime Date { get; set; }

        public string UserName { get; set; }
    }

    public class SecLogContext: DbContext
    {
        public SecLogContext() : base("SecLogDb")
        {
            Database.SetInitializer<SecLogContext>(new DropCreateDatabaseIfModelChanges<SecLogContext>());
        }

        public DbSet<SecLog> SecLogs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}