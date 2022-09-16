using BrinksAPI.Entities;
using BrinksAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BrinksAPI.Auth
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
        public DbSet<User> users { get; set; }
        public DbSet<AuthenticationLevel> AuthenticationLevels { get; set; }

        #region Mapping tables
        public DbSet<Entities.ServiceLevel> serviceLevels { get; set; }
        public DbSet<Entities.DocumentType> documentTypes { get; set; } 
        public DbSet<Entities.ActionType> actionTypes { get; set; } 
        // Sites with server ID needed for shipment history and mawb history (only 65 sites)
        public DbSet<Entities.Site> sites { get; set; }
        // Sites without serverID needed for organization (800+ sites)
        public DbSet<Entities.OrganizationSite> organizationSites { get; set; } 
        // Default Unloco for Organizations
        public DbSet<Entities.OrganizationUnloco> organizationUnloco { get; set; } 
        public DbSet<Entities.RiskCodeDescription> riskCodeDescriptions { get; set; } 
        public DbSet<Entities.EventCode> eventCodes { get; set; } 
        public DbSet<Entities.TransportMode> transportModes { get; set; } 
        public DbSet<Entities.PackageType> packageTypes { get; set; } 
        #endregion
        public DbSet<Document> documents { get; set; }
        public DbSet<TransportBooking> transportBookings { get; set; }
    }
}
