﻿using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Data.Entity.ModelConfiguration.Conventions;
using System;
using System.ComponentModel.DataAnnotations;

namespace TFA.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }

        public string SerialHash { get; set; }
        public DateTime PasswordResetDate { get; set; }
        [Required]
        public bool ThreeFactorEnabled { get; set; }

        public string VCode { get; set; }

        // Notifications
        public bool ChangePasswordSMS { get; set; }
        public bool ChangePasswordEmail { get; set; }
        public bool ChangePhoneNumberSMS { get; set; }
        public bool ChangePhoneNumberEmail { get; set; }
        public bool AccountLockoutSMS { get; set; }
        public bool AccountLockoutEmail { get; set; }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("TFAAccount", throwIfV1Schema: false)
        {
            //Database.SetInitializer<ApplicationDbContext>(new DropCreateDatabaseAlways<ApplicationDbContext>());
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<IdentityUser>()
                .ToTable("TFAUser");

            modelBuilder.Entity<IdentityRole>()
                .ToTable("TFARole");

            modelBuilder.Entity<IdentityUserRole>()
                .HasKey(r => new { r.UserId, r.RoleId })
                .ToTable("TFAUserRole");

            modelBuilder.Entity<IdentityUserLogin>()
                .HasKey(l => new { l.LoginProvider, l.ProviderKey, l.UserId })
                .ToTable("TFAUserLogin");

            modelBuilder.Entity<IdentityUserClaim>()
                .ToTable("TFAUserClaim");
        }
    }
}